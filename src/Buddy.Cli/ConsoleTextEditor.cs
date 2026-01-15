using System.Text;
using Buddy.Cli.Commands;
using Spectre.Console;

namespace Buddy.Cli;

internal sealed class ConsoleTextEditor {
    private readonly string _promptMarkup;
    private readonly string _promptPlain;
    private readonly TextBuffer _buffer = new();
    private readonly ConsoleTextRenderer _renderer;
    private readonly SlashCommandRegistry _commandRegistry;
    private int? _preferredColumn;
    private List<string>? _autocompleteMatches;
    private int _autocompleteIndex;
    private int _autocompleteLineStart;

    public ConsoleTextEditor(string promptMarkup, string promptPlain, SlashCommandRegistry commandRegistry) {
        _promptMarkup = promptMarkup;
        _promptPlain = promptPlain;
        _renderer = new ConsoleTextRenderer(_promptMarkup, _promptPlain);
        _commandRegistry = commandRegistry;
    }

    public string ReadInput() {
        _renderer.Begin();
        _renderer.Render(_buffer.Text, _buffer.CursorIndex);

        while (true) {
            var key = Console.ReadKey(intercept: true);
            
            // Handle CSI u (extended keyboard protocol) sequences from terminals like Ghostty
            // These start with ESC [ and need special parsing
            if (key.Key == ConsoleKey.Escape && Console.KeyAvailable) {
                var parsed = TryParseCsiSequence();
                if (parsed.HasValue) {
                    key = parsed.Value;
                }
            }
            
            if (HandleKey(key, out var shouldSubmit)) {
                if (shouldSubmit) {
                    _renderer.End();
                    return _buffer.Text.TrimEnd('\r', '\n');
                }

                _renderer.Render(_buffer.Text, _buffer.CursorIndex);
            }
        }
    }
    
    /// <summary>
    /// Tries to parse a CSI sequence (ESC [ ... ) from terminals using extended keyboard protocols.
    /// Returns a synthesized ConsoleKeyInfo if successful, null otherwise.
    /// </summary>
    private ConsoleKeyInfo? TryParseCsiSequence() {
        // We've already read ESC, now check for [
        if (!Console.KeyAvailable) return null;
        
        var next = Console.ReadKey(intercept: true);
        if (next.KeyChar != '[') {
            // Not a CSI sequence - this was just Escape followed by something else
            // We can't "unread" so we'll lose this key, but it's rare
            return null;
        }
        
        // Read until we get a terminator (letter or ~)
        var sequence = new StringBuilder();
        while (Console.KeyAvailable) {
            var ch = Console.ReadKey(intercept: true);
            if (char.IsLetter(ch.KeyChar) || ch.KeyChar == '~') {
                sequence.Append(ch.KeyChar);
                break;
            }
            sequence.Append(ch.KeyChar);
        }
        
        var seq = sequence.ToString();
        
        // Parse CSI u format: <keycode>;<modifiers>;<text>~ or <keycode>;<modifiers>u
        // For Enter in Ghostty: "27;2;13~" where 13 is CR
        var parts = seq.TrimEnd('~', 'u').Split(';');
        
        if (parts.Length >= 1) {
            // Try to extract the keycode - it might be in different positions
            // depending on the sequence format
            int keyCode = 0;
            ConsoleModifiers modifiers = ConsoleModifiers.None;
            
            if (parts.Length >= 3 && int.TryParse(parts[2], out var textCode)) {
                // CSI <keycode> ; <modifiers> ; <text> ~ format
                keyCode = textCode;
                if (int.TryParse(parts[1], out var mod)) {
                    modifiers = ParseCsiModifiers(mod);
                }
            } else if (parts.Length >= 1 && int.TryParse(parts[0], out var code)) {
                keyCode = code;
                if (parts.Length >= 2 && int.TryParse(parts[1], out var mod)) {
                    modifiers = ParseCsiModifiers(mod);
                }
            }
            
            // Convert keycode to ConsoleKeyInfo
            if (keyCode == 13) { // CR - Enter
                return new ConsoleKeyInfo('\r', ConsoleKey.Enter, 
                    modifiers.HasFlag(ConsoleModifiers.Shift),
                    modifiers.HasFlag(ConsoleModifiers.Alt),
                    modifiers.HasFlag(ConsoleModifiers.Control));
            }
        }
        
        return null;
    }
    
    private static ConsoleModifiers ParseCsiModifiers(int mod) {
        // CSI u modifier encoding: 1 + (shift ? 1 : 0) + (alt ? 2 : 0) + (ctrl ? 4 : 0) + (meta ? 8 : 0)
        // So mod=2 means shift (1 + 1)
        var modifiers = ConsoleModifiers.None;
        mod -= 1; // Remove the base 1
        if ((mod & 1) != 0) modifiers |= ConsoleModifiers.Shift;
        if ((mod & 2) != 0) modifiers |= ConsoleModifiers.Alt;
        if ((mod & 4) != 0) modifiers |= ConsoleModifiers.Control;
        return modifiers;
    }

    private bool HandleKey(ConsoleKeyInfo key, out bool submit) {
        submit = false;

        if (key.Key != ConsoleKey.Tab) {
            ResetAutocomplete();
        }

        // Handle Enter key - check both Key property and KeyChar for terminal compatibility
        // Some terminals may not set ConsoleKey.Enter but will have '\r' or '\n' as KeyChar
        var isEnter = key.Key == ConsoleKey.Enter || key.KeyChar == '\r' || key.KeyChar == '\n';
        if (isEnter) {
            if ((key.Modifiers & ConsoleModifiers.Control) != 0 || (key.Modifiers & ConsoleModifiers.Shift) != 0) {
                InsertNewline();
                return true;
            }

            if (_buffer.CursorIndex == _buffer.Length && _buffer.Length > 0 && _buffer.Text[^1] == '\\') {
                _buffer.Backspace();
                InsertNewline();
                return true;
            }

            submit = true;
            return true;
        }

        if (key.Key == ConsoleKey.J && (key.Modifiers & ConsoleModifiers.Control) != 0) {
            InsertNewline();
            return true;
        }

        if (key.Key == ConsoleKey.Tab && (key.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Shift | ConsoleModifiers.Alt)) == 0) {
            return TryAutocomplete();
        }

        if (key.Key == ConsoleKey.LeftArrow) {
            if (_buffer.MoveLeft()) {
                _preferredColumn = null;
                return true;
            }

            return false;
        }

        if (key.Key == ConsoleKey.RightArrow) {
            if (_buffer.MoveRight()) {
                _preferredColumn = null;
                return true;
            }

            return false;
        }

        if (key.Key == ConsoleKey.UpArrow) {
            MoveVertical(-1);
            return true;
        }

        if (key.Key == ConsoleKey.DownArrow) {
            MoveVertical(1);
            return true;
        }

        if (key.Key == ConsoleKey.Home) {
            var lineStart = _buffer.GetLineStart(_buffer.CursorIndex);
            _buffer.SetCursor(lineStart);
            _preferredColumn = 0;
            return true;
        }

        if (key.Key == ConsoleKey.End) {
            var lineEnd = _buffer.GetLineEnd(_buffer.CursorIndex);
            _buffer.SetCursor(lineEnd);
            _preferredColumn = lineEnd - _buffer.GetLineStart(_buffer.CursorIndex);
            return true;
        }

        if (key.Key == ConsoleKey.Delete) {
            if (_buffer.Delete()) {
                _preferredColumn = null;
                return true;
            }

            return false;
        }

        if (key.Key == ConsoleKey.Backspace) {
            if (_buffer.Backspace()) {
                _preferredColumn = null;
                return true;
            }

            return false;
        }

        if (key.Key == ConsoleKey.A && (key.Modifiers & ConsoleModifiers.Control) != 0) {
            _buffer.SetCursor(0);
            _preferredColumn = 0;
            return true;
        }

        if (key.Key == ConsoleKey.E && (key.Modifiers & ConsoleModifiers.Control) != 0) {
            _buffer.SetCursor(_buffer.Length);
            _preferredColumn = _buffer.GetLineColumn(_buffer.CursorIndex);
            return true;
        }

        if ((key.Modifiers & ConsoleModifiers.Control) != 0) {
            return false;
        }

        if (key.KeyChar == '\u0000') {
            return false;
        }

        _buffer.Insert(key.KeyChar);
        _preferredColumn = null;
        return true;
    }

    private void ResetAutocomplete() {
        _autocompleteMatches = null;
        _autocompleteIndex = 0;
        _autocompleteLineStart = 0;
    }

    private bool TryAutocomplete() {
        var lineStart = _buffer.GetLineStart(_buffer.CursorIndex);

        if (_autocompleteMatches is not null && _autocompleteMatches.Count > 0 && _autocompleteLineStart == lineStart) {
            _autocompleteIndex = (_autocompleteIndex + 1) % _autocompleteMatches.Count;
            ApplyCompletion(lineStart, _autocompleteMatches[_autocompleteIndex]);
            return true;
        }

        var lineEnd = _buffer.GetLineEnd(_buffer.CursorIndex);
        var lineText = _buffer.Text.Substring(lineStart, lineEnd - lineStart);
        if (!lineText.StartsWith("/", StringComparison.Ordinal)) {
            return false;
        }

        var cursorColumn = _buffer.CursorIndex - lineStart;
        var commandEnd = lineText.IndexOf(' ');
        if (commandEnd == -1) {
            commandEnd = lineText.Length;
        }

        if (cursorColumn > commandEnd) {
            return false;
        }

        var prefix = lineText.Substring(0, cursorColumn);
        var matches = _commandRegistry.GetCompletions(prefix);
        if (matches.Count == 0) {
            return false;
        }

        _autocompleteMatches = matches.ToList();
        _autocompleteIndex = 0;
        _autocompleteLineStart = lineStart;
        ApplyCompletion(lineStart, _autocompleteMatches[_autocompleteIndex]);
        return true;
    }

    private void ApplyCompletion(int lineStart, string completion) {
        var lineEnd = _buffer.GetLineEnd(lineStart);
        var lineText = _buffer.Text.Substring(lineStart, lineEnd - lineStart);
        var commandEnd = lineText.IndexOf(' ');
        if (commandEnd == -1) {
            commandEnd = lineText.Length;
        }

        _buffer.ReplaceRange(lineStart, commandEnd, completion);
        _preferredColumn = null;
    }

    private void InsertNewline() {
        _buffer.Insert('\n');
        _preferredColumn = null;
    }

    private void MoveVertical(int delta) {
        var lineStarts = _buffer.GetLineStarts();
        var lineIndex = _buffer.GetLineIndex(_buffer.CursorIndex, lineStarts);
        var column = _preferredColumn ?? _buffer.GetLineColumn(_buffer.CursorIndex, lineStarts);

        var targetLine = lineIndex + delta;
        if (targetLine < 0 || targetLine >= lineStarts.Count) {
            return;
        }

        var targetStart = lineStarts[targetLine];
        var targetEnd = _buffer.GetLineEnd(targetStart, lineStarts);
        var targetColumn = Math.Min(column, targetEnd - targetStart);
        _buffer.SetCursor(targetStart + targetColumn);
        _preferredColumn = column;
    }

    private sealed class TextBuffer {
        private readonly StringBuilder _buffer = new();
        private int _cursor;

        public int CursorIndex => _cursor;
        public int Length => _buffer.Length;
        public string Text => _buffer.ToString();

        public void Insert(char ch) {
            _buffer.Insert(_cursor, ch);
            _cursor++;
        }

        public bool Backspace() {
            if (_cursor == 0) {
                return false;
            }

            _buffer.Remove(_cursor - 1, 1);
            _cursor--;
            return true;
        }

        public bool Delete() {
            if (_cursor >= _buffer.Length) {
                return false;
            }

            _buffer.Remove(_cursor, 1);
            return true;
        }

        public bool MoveLeft() {
            if (_cursor == 0) {
                return false;
            }

            _cursor--;
            return true;
        }

        public bool MoveRight() {
            if (_cursor >= _buffer.Length) {
                return false;
            }

            _cursor++;
            return true;
        }

        public void SetCursor(int index) {
            _cursor = Math.Clamp(index, 0, _buffer.Length);
        }

        public void ReplaceRange(int start, int length, string text) {
            start = Math.Clamp(start, 0, _buffer.Length);
            length = Math.Clamp(length, 0, _buffer.Length - start);

            _buffer.Remove(start, length);
            _buffer.Insert(start, text);
            _cursor = start + text.Length;
        }

        public List<int> GetLineStarts() {
            var starts = new List<int> { 0 };
            for (var i = 0; i < _buffer.Length; i++) {
                if (_buffer[i] == '\n') {
                    starts.Add(i + 1);
                }
            }
            return starts;
        }

        public int GetLineIndex(int cursorIndex) {
            return GetLineIndex(cursorIndex, GetLineStarts());
        }

        public int GetLineIndex(int cursorIndex, List<int> lineStarts) {
            var index = 0;
            for (var i = 0; i < lineStarts.Count; i++) {
                if (lineStarts[i] <= cursorIndex) {
                    index = i;
                }
            }
            return index;
        }

        public int GetLineStart(int cursorIndex) {
            var starts = GetLineStarts();
            return starts[GetLineIndex(cursorIndex, starts)];
        }

        public int GetLineEnd(int cursorIndex) {
            var starts = GetLineStarts();
            var lineIndex = GetLineIndex(cursorIndex, starts);
            return GetLineEnd(starts[lineIndex], starts);
        }

        public int GetLineEnd(int lineStart, List<int> lineStarts) {
            var lineIndex = lineStarts.IndexOf(lineStart);
            if (lineIndex == -1) {
                return _buffer.Length;
            }

            if (lineIndex == lineStarts.Count - 1) {
                return _buffer.Length;
            }

            return lineStarts[lineIndex + 1] - 1;
        }

        public int GetLineColumn(int cursorIndex) {
            var starts = GetLineStarts();
            return GetLineColumn(cursorIndex, starts);
        }

        public int GetLineColumn(int cursorIndex, List<int> lineStarts) {
            var lineStart = lineStarts[GetLineIndex(cursorIndex, lineStarts)];
            return cursorIndex - lineStart;
        }
    }

    private sealed class ConsoleTextRenderer {
        private readonly string _promptMarkup;
        private readonly string _promptPlain;
        private int _cursorLine;        // Which line cursor is currently on (0-indexed)
        private int _contentLineCount;  // How many lines of content we last rendered
        private bool _started;

        // ANSI escape sequences
        private const string Esc = "\x1b";
        private const string EraseLine = Esc + "[2K";     // Erase entire line
        private const string CursorUp = Esc + "[A";       // Move cursor up
        
        private static string CursorToColumn(int col) => $"{Esc}[{col + 1}G";

        public ConsoleTextRenderer(string promptMarkup, string promptPlain) {
            _promptMarkup = promptMarkup;
            _promptPlain = promptPlain;
        }

        public void Begin() {
            if (_started) {
                return;
            }
            _started = true;
            _cursorLine = 0;
            _contentLineCount = 1;
        }

        public void End() {
            // After rendering, cursor could be anywhere in our rendered content.
            // Move to column 0 and write a newline to start fresh
            Console.Write('\r');
            Console.WriteLine();
            
            _started = false;
            _cursorLine = 0;
            _contentLineCount = 1;
        }

        public void Render(string text, int cursorIndex) {
            if (!_started) {
                return;
            }

            var lines = text.Replace("\r", string.Empty).Split('\n');
            var newLineCount = lines.Length;
            
            // We need to clear max of previous content lines and new content lines
            var maxLines = Math.Max(_contentLineCount, newLineCount);

            // Move cursor to start of our rendering area (go up to line 0)
            Console.Out.Write("\r"); // Go to column 0
            for (var i = 0; i < _cursorLine; i++) {
                Console.Out.Write(CursorUp);
            }
            
            // Now we're at start of first line - clear and rewrite all lines
            for (var i = 0; i < maxLines; i++) {
                Console.Out.Write(EraseLine);
                Console.Out.Write("\r");
                
                if (i < lines.Length) {
                    Console.Out.Flush();
                    AnsiConsole.Markup(_promptMarkup);
                    Console.Out.Write(lines[i]);
                }
                
                // Move to next line (except for last)
                if (i < maxLines - 1) {
                    Console.Out.Write("\n");
                }
            }
            
            // After the loop, cursor is at line (maxLines - 1)
            // Remember how many content lines we have
            _contentLineCount = newLineCount;
            
            // Position cursor at the correct location in the content
            _cursorLine = PositionCursor(lines, cursorIndex, maxLines - 1);
        }

        /// <summary>
        /// Positions the cursor and returns the line index where cursor ends up.
        /// </summary>
        private int PositionCursor(string[] lines, int cursorIndex, int currentLine) {
            // Find which line the cursor is on
            var pos = 0;
            var lineIndex = 0;
            for (var i = 0; i < lines.Length; i++) {
                var lineEnd = pos + lines[i].Length;
                if (cursorIndex <= lineEnd) {
                    lineIndex = i;
                    break;
                }
                pos = lineEnd + 1; // +1 for newline
                lineIndex = i;
            }

            var lineStart = 0;
            for (var i = 0; i < lineIndex; i++) {
                lineStart += lines[i].Length + 1;
            }

            var column = cursorIndex - lineStart;
            var targetCol = _promptPlain.Length + column;
            
            // We're at currentLine. Move up to target line.
            var linesToMoveUp = currentLine - lineIndex;
            
            Console.Out.Write("\r");
            for (var i = 0; i < linesToMoveUp; i++) {
                Console.Out.Write(CursorUp);
            }
            
            Console.Out.Write(CursorToColumn(targetCol));
            Console.Out.Flush();
            
            return lineIndex;
        }
    }
}
