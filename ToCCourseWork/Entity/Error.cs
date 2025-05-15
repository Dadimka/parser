

namespace ToCCourseWork.Entity
{
    public class Error : IComparable<Error>
    {
        private string _errorText;
        private int _line;
        private int _column;

        

        public string ErrorText { get { return _errorText; } }

        public int Line { get { return _line; } }

        public int Column { get { return _column; } }

        public Error(string errorText, int line, int column)
        {
            _errorText = errorText;
            _line = line;
            _column = column;
        }
        public int CompareTo(Error? other)
        {
            if (other == null) return 1;

            // Сначала сравниваем по строке
            int lineComparison = _line.CompareTo(other._line);
            if (lineComparison != 0)
            {
                return lineComparison;
            }

            // Если строки равны, сравниваем по столбцу
            return _column.CompareTo(other._column);
        }

        public override string ToString()
        {
            return $"{_errorText} в строке {_line}, столбце {_column}";
        }

    }
}
