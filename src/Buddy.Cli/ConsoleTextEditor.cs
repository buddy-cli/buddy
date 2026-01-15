using System.Text;
using Spectre.Console;

namespace Buddy.Cli;

internal sealed class ConsoleTextEditor {
    private readonly string _promptMarkup;
    private readonly string _promptPlain;
    private readonly TextBuffer _buffer = new();
    private readonly ConsoleTextRenderer _renderer;
    private int? _preferredColumn;

    public ConsoleTextEditor(string promptMarkup, string promptPlain) {
        _promptMarkup = promptMarkup;
        _promptPlain = promptPlain;
        _renderer = new ConsoleTextRenderer(_promptMarkup, _promptPlain);
    }

    public string ReadInput() {
        _renderer.Begin();
        _renderer.Render(_buffer.Text, _buffer.CursorIndex);

        while (true) {
            var key = Console.ReadKey(intercept: true);
            if (HandleKey(key, out var shouldSubmit)) {
                if (shouldSubmit) {
                    _renderer.End();
                    return _buffer.Text.TrimEnd('\r', '\n');
                }

                _renderer.Render(_buffer.Text, _buffer.CursorIndex);
            }
        }
    }

    private bool HandleKey(ConsoleKeyInfo key, out bool submit) {
        submit = false;

        if (key.Key == ConsoleKey.Enter) {
            if ((key.Modifiers & ConsoleModifiers.Control) != 0 || (key.Modifiers & ConsoleModifiers.Shift) != 0 || key.KeyChar == '\\') {
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
        private int _originTop;
        private int _renderedLines = 1;
        private bool _started;

        public ConsoleTextRenderer(string promptMarkup, string promptPlain) {
            _promptMarkup = promptMarkup;
            _promptPlain = promptPlain;
        }

        public void Begin() {
            if (_started) {
                return;
            }

            _originTop = Console.CursorTop;
            _started = true;
        }

        public void End() {
            // Move cursor to end of last rendered line and print newline
            Console.SetCursorPosition(0, _originTop + _renderedLines - 1);
            Console.WriteLine();
            _started = false;
            _renderedLines = 1;
        }

        public void Render(string text, int cursorIndex) {
            if (!_started) {
                return;
            }

            var lines = text.Replace("\r", string.Empty).Split('\n');
            var newLineCount = lines.Length;

            // Check if we need more lines than before and might scroll
            if (newLineCount > _renderedLines) {
                // Position at end and write new lines to make space
                Console.SetCursorPosition(0, _originTop + _renderedLines - 1);
                for (var i = _renderedLines; i < newLineCount; i++) {
                    Console.WriteLine();
                }
                // Adjust origin if console scrolled
                var expectedBottom = _originTop + newLineCount - 1;
                if (expectedBottom >= Console.BufferHeight) {
                    _originTop -= (expectedBottom - Console.BufferHeight + 1);
                    if (_originTop < 0) _originTop = 0;
                }
            }

            ClearLines(newLineCount);
            WriteText(lines);
            _renderedLines = newLineCount;
            PositionCursor(lines, cursorIndex);
        }

        private void ClearLines(int lineCount) {
            var width = Math.Max(Console.BufferWidth, 1);
            var clear = new string(' ', Math.Max(0, width - 1));

            // Clear all lines we might have used (max of old and new count)
            var maxLines = Math.Max(_renderedLines, lineCount);
            for (var i = 0; i < maxLines; i++) {
                var top = _originTop + i;
                if (top >= Console.BufferHeight) {
                    break;
                }

                Console.SetCursorPosition(0, top);
                Console.Write(clear);
            }
        }

        private void WriteText(string[] lines) {
            for (var i = 0; i < lines.Length; i++) {
                Console.SetCursorPosition(0, _originTop + i);
                AnsiConsole.Markup(_promptMarkup);
                Console.Write(lines[i]);
            }
        }

        private void PositionCursor(string[] lines, int cursorIndex) {
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
            var left = _promptPlain.Length + column;
            var top = _originTop + lineIndex;

            if (top >= Console.BufferHeight) {
                top = Console.BufferHeight - 1;
            }

            var maxLeft = Math.Max(Console.BufferWidth - 1, 0);
            if (left > maxLeft) {
                left = maxLeft;
            }

            Console.SetCursorPosition(left, top);
        }
    }
}
