

namespace ToCCourseWork.Entity
{
    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        public int Line { get; }
        public int StartColumn { get; }
        public int EndColumn { get; }

        public Token(TokenType type, string value, int line, int startColumn, int endColumn)
        {
            Type = type;
            Value = value;
            Line = line;
            StartColumn = startColumn;
            EndColumn = endColumn;
        }

        public override string ToString()
        {
            if (Value.Contains("\n")) {
                return $"{Type}: '\\n' в строке: {Line}, положение: {StartColumn}";
            }
            return $"{Type}: '{Value}' в строке: {Line}, положение: {StartColumn}";
        }
    }
}
