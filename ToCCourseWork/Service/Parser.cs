using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using ToCCourseWork.Entity;

namespace ToCCourseWork.Service
{
    public class Parser
    {
        private readonly List<Token> tokens;
        private readonly List<Error> errors;

        public Parser(List<Token> tokens, List<Error> errors)
        {
            this.tokens = tokens;
            this.errors = errors;
        }


        public List<string> Parse(TextBox textBox)
        {
            List<string> results = new List<string>();
            string text = textBox.Text;

            Regex identifierRegex = new Regex(@"\b[$_\w][\w]*\b"); // Более упрощенный вариант для Python-like
            
            MatchCollection identifierMatches = identifierRegex.Matches(text);
            foreach (Match match in identifierMatches)
            {
                results.Add($"Найдено совпадение Идентификатор - {match.Value} - {match.Index}");
            }

            // 2. Многострочные комментарии (Python)
            Regex multilineCommentRegex = new Regex("\"\"\"[\\s\\S]*?\"\"\"");
            MatchCollection multilineCommentMatches = multilineCommentRegex.Matches(text);
            foreach (Match match in multilineCommentMatches)
            {
                results.Add($"Найдено совпадение Многострочный комментарий - {match.Value} - {match.Index}");
            }
            //  3. VIN (Vehicle Identification Number)
            Regex VINRegex = new Regex(@"\b[A-HJ-NPR-Z0-9]{17}\b");  
            MatchCollection vinMatches = VINRegex.Matches(text);
            foreach (Match match in vinMatches)
            {
                results.Add($"Найдено совпадение VIN - {match.Value} - {match.Index}");
            }

            return results;
        }
        //Старый parse
        //public List<Error> Parse()
        //{

        //    List<Error> errors = [.. this.errors];
        //    foreach (List<Token> line in SplitTokensIntoLines(tokens))
        //    {
        //        RecursiveParser recursiveParser = new RecursiveParser(line);
        //        errors.AddRange(recursiveParser.Parse());
        //    }


        //    return errors
        //        .OrderBy(e => e.Line)
        //        .ThenBy(e => e.Column)
        //        .ToList();
        //}


        private List<List<Token>> SplitTokensIntoLines(List<Token> tokens)
        {
            List<List<Token>> lines = new List<List<Token>>();
            List<Token> currentLine = new List<Token>();

            foreach (Token token in tokens)
            {
                currentLine.Add(token);

                if (token.Type == TokenType.END)
                {
                    lines.Add(currentLine);
                    currentLine = new List<Token>();
                }
            }

            if (currentLine.Count > 0)
            {
                lines.Add(currentLine);
            }

            return lines;
        }
    }


    public class RecursiveParser
    {
        private readonly List<Token> tokens;
        private int OpenBracket = 0;

        public RecursiveParser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        public List<Error> Parse()
        {
            int currentPosition = 0;
            List<Error> errors = new List<Error>();
            return Var(currentPosition, errors);
        }

        private List<Error> Var(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return errors;
            }

            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                currentPosition++;

            if (!Match(currentPosition, TokenType.IDENTIFIER, errors))
            {
                return GetMinErrorList(
                        Equals(currentPosition, CreateErrorList(currentPosition, TokenType.IDENTIFIER, ErrorType.PUSH, errors)),
                        Equals(currentPosition + 1, CreateErrorList(currentPosition, TokenType.IDENTIFIER, ErrorType.REPLACE, errors)),
                        Var(currentPosition + 1, CreateErrorList(currentPosition, TokenType.IDENTIFIER, ErrorType.DELETE, errors))
                );
            }

            return Equals(currentPosition + 1, errors);
        }

        private List<Error> Equals(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return errors;
            }

            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                currentPosition++;

            if (!Match(currentPosition, TokenType.EQUAL, errors))
            {
                return GetMinErrorList(
                        Lambda(currentPosition, CreateErrorList(currentPosition, TokenType.EQUAL, ErrorType.PUSH, errors)),
                        Lambda(currentPosition + 1, CreateErrorList(currentPosition, TokenType.EQUAL, ErrorType.REPLACE, errors)),
                        Equals(currentPosition + 1, CreateErrorList(currentPosition, TokenType.EQUAL, ErrorType.DELETE, errors))
                );
            }
            return Lambda(currentPosition + 1, errors);
        }

        private List<Error> Lambda(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return errors;
            }

            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                currentPosition++;

            if (!Match(currentPosition, TokenType.LAMBDA, errors))
            {
                return GetMinErrorList(
                        Argument(currentPosition, CreateErrorList(currentPosition, TokenType.LAMBDA, ErrorType.PUSH, errors)),
                        Argument(currentPosition + 1, CreateErrorList(currentPosition, TokenType.LAMBDA, ErrorType.REPLACE, errors)),
                        Lambda(currentPosition + 1, CreateErrorList(currentPosition, TokenType.LAMBDA, ErrorType.DELETE, errors))
                );
            }
            return Argument(currentPosition + 1, errors);
        }

        private List<Error> Argument(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return errors;
            }

            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                currentPosition++;

            if(Match(currentPosition, TokenType.COLON, errors)) {
                (errors, currentPosition) = Expression(currentPosition + 1, errors);
                return End(currentPosition, errors);

            } else 
            {
                return ArgumentName(currentPosition, errors);
            }
            
        }

        private List<Error> ArgumentName(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return errors;
            }

            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                currentPosition++;

            if (!Match(currentPosition, TokenType.IDENTIFIER, errors))
            {
                return GetMinErrorList(
                        CommaOrColon(currentPosition + 1, CreateErrorList(currentPosition, TokenType.IDENTIFIER, ErrorType.REPLACE, errors)).Item1,
                        ArgumentName(currentPosition + 1, CreateErrorList(currentPosition, TokenType.IDENTIFIER, ErrorType.DELETE, errors)),
                        CommaOrColon(currentPosition, CreateErrorList(currentPosition, TokenType.IDENTIFIER, ErrorType.PUSH, errors)).Item1
                );
            }
            return CommaOrColon(currentPosition + 1, errors).Item1;
        }

        private (List<Error>, int) CommaOrColon(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return (errors, currentPosition);
            }

            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                currentPosition++;

            if (Match(currentPosition, TokenType.COMMA, errors))
            {
                return (Argument(currentPosition + 1, errors), currentPosition);
            }
            else if (Match(currentPosition, TokenType.COLON,errors))
            {
                (errors, currentPosition) = Expression(currentPosition + 1, errors);
                return (End(currentPosition, errors), currentPosition);
            }

            else if (IsContainToken(TokenType.COLON, currentPosition) )
            {
                if(errors.Count > 30) return (errors, currentPosition);
                errors = GetMinErrorList(
                        Argument(currentPosition, CreateErrorList(currentPosition, TokenType.COMMA, ErrorType.PUSH, errors)),
                        Argument(currentPosition + 1, CreateErrorList(currentPosition, TokenType.COMMA, ErrorType.REPLACE, errors)),
                        CommaOrColon(currentPosition + 1, CreateErrorList(currentPosition, TokenType.COMMA, ErrorType.DELETE, errors)).Item1
                );
                return (errors, currentPosition);
            }

            else
            {
                (errors, currentPosition) = GetMinErrorList(
                        Expression(currentPosition, CreateErrorList(currentPosition, TokenType.COLON, ErrorType.PUSH, errors)),
                        Expression(currentPosition + 1, CreateErrorList(currentPosition, TokenType.COLON, ErrorType.REPLACE, errors)),
                        CommaOrColon(currentPosition + 1, CreateErrorList(currentPosition, TokenType.COLON, ErrorType.DELETE, errors))
                );
                return (End(currentPosition, errors), currentPosition);
            }
        }

        private (List<Error>, int) Expression(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return (errors, currentPosition);
            }


            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return Expression(currentPosition + 1, errors);

            (errors, currentPosition) = T(currentPosition, errors);
            return A(currentPosition, errors);
        }

        private (List<Error>, int) T(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return (errors, currentPosition);
            }
            (errors, currentPosition) = O(currentPosition, errors);
            return B(currentPosition, errors);
        }

        private (List<Error>, int) A(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return (errors, currentPosition);
            }

            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return Expression(currentPosition + 1, errors);

            if (Match(currentPosition, TokenType.NUMBER, errors) ||
                Match(currentPosition, TokenType.IDENTIFIER, errors) || 
                Match(currentPosition, TokenType.OPEN_BRACKET, errors))
            {
                AddError(currentPosition, TokenType.PLUS_OPERATION, ErrorType.PUSH, errors);
                (errors, currentPosition) = T(currentPosition, errors);
                return A(currentPosition, errors);

            } else if (Match(currentPosition, TokenType.PLUS_OPERATION, errors))
            {
                (errors, currentPosition) = T(currentPosition + 1, errors);
                return A(currentPosition, errors);
            } else if (OpenBracket == 0 && Match(currentPosition, TokenType.CLOSE_BRACKET,errors) &&
                (Match(currentPosition + 1, TokenType.PLUS_OPERATION, errors) || Match(currentPosition + 1, TokenType.CLOSE_BRACKET, errors)))
            {
                AddError(currentPosition, TokenType.PLUS_OPERATION, ErrorType.DELETE, errors);
                return A(currentPosition+1,errors);
            } else
            {
                return (errors, currentPosition);
            }


        }

        private (List<Error>, int) O(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return (errors, currentPosition);
            }


            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return Expression(currentPosition + 1, errors);

            if (Match(currentPosition, TokenType.IDENTIFIER, errors) || Match(currentPosition, TokenType.NUMBER, errors))
            {
                return (errors, currentPosition + 1);
            } else if (Match(currentPosition, TokenType.OPEN_BRACKET, errors))
            {
                OpenBracket++;
                (errors, currentPosition) = Expression(currentPosition + 1, errors);
                OpenBracket--;
                return CloseBracket(currentPosition, errors);
            }
            else
            {
                if (OpenBracket == 0 && Match(currentPosition, TokenType.CLOSE_BRACKET, errors))
                {
                    AddError(currentPosition, TokenType.IDENTIFIER, ErrorType.REPLACE, errors);
                    return (errors, currentPosition + 1);
                } else
                {
                    AddError(currentPosition, TokenType.IDENTIFIER, ErrorType.PUSH, errors);
                }
            }
            return (errors, currentPosition);
        }

        private (List<Error>, int) B(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return (errors, currentPosition);
            }

            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return Expression(currentPosition + 1, errors);

            if (Match(currentPosition, TokenType.NUMBER, errors) ||
                Match(currentPosition, TokenType.IDENTIFIER, errors) ||
                Match(currentPosition, TokenType.OPEN_BRACKET, errors))
            {
                AddError(currentPosition, TokenType.MULTIPLICATION_OPERATION, ErrorType.PUSH, errors);
                (errors, currentPosition) = O(currentPosition, errors);
                return B(currentPosition, errors);

            }
            else if (Match(currentPosition, TokenType.MULTIPLICATION_OPERATION, errors))
            {
                (errors, currentPosition) = O(currentPosition + 1, errors);
                return B(currentPosition, errors);
            }
            else if (OpenBracket == 0 && Match(currentPosition, TokenType.CLOSE_BRACKET, errors) && 
                (Match(currentPosition+1, TokenType.MULTIPLICATION_OPERATION, errors) || Match(currentPosition + 1, TokenType.CLOSE_BRACKET, errors)))
            {
                AddError(currentPosition, TokenType.PLUS_OPERATION, ErrorType.DELETE, errors);
                return B(currentPosition + 1 ,errors);
            }
            else
            {
                return (errors, currentPosition);
            }
        }

        private (List<Error>, int) CloseBracket(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                AddError(currentPosition - 1, TokenType.CLOSE_BRACKET, ErrorType.PUSH, errors);
                return (errors, currentPosition);
            }


            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return Expression(currentPosition + 1, errors);

            if (!Match(currentPosition, TokenType.CLOSE_BRACKET, errors))
            {
                AddError(currentPosition, TokenType.CLOSE_BRACKET, ErrorType.PUSH, errors);
            }
            return (errors, currentPosition + 1);
        }


        private List<Error> End(int currentPosition, List<Error> errors)
        {

            if (IsAtEnd(currentPosition))
            {
                if (!Match(currentPosition - 1, TokenType.END, errors))
                {
                    AddError(currentPosition - 1, TokenType.END, ErrorType.PUSH, errors);
                }
                return errors;
            }
            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return End(currentPosition + 1, errors);
            if (!Match(currentPosition, TokenType.END, errors))
            {
                AddError(currentPosition, TokenType.END, ErrorType.DELETE, errors);
                End(currentPosition + 1, errors);
            }
            return errors;
        }


        private bool Match(int currentPosition, TokenType expectedTokentype, List<Error> errors)
        {
            return Check(currentPosition, expectedTokentype, errors);
        }

        private bool Check(int currentPosition, TokenType expectedTokenType, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return false;
            }
            else
            {
                return GetToken(currentPosition).Type == expectedTokenType;
            }
        }

        private List<Error> CreateErrorList(
                int currentPosition,
                TokenType expectedTokenType,
                ErrorType errorType,
                List<Error> errorEntities
        )
        {
            List<Error> errors = new List<Error>(errorEntities);
            AddError(currentPosition, expectedTokenType, errorType, errors);
            return errors;
        }

        private List<Error> GetMinErrorList(List<Error> e1, List<Error> e2, List<Error> e3)
        {
            if (e1.Count <= e2.Count && e1.Count <= e3.Count)
            {
                return e1;
            }
            else if (e2.Count <= e1.Count && e2.Count <= e3.Count)
            {
                return e2;
            }
            else
            {
                return e3;
            }
        }

        private (List<Error>, int) GetMinErrorList(
            (List<Error> Errors, int Count) tuple1,
            (List<Error> Errors, int Count) tuple2,
            (List<Error> Errors, int Count) tuple3)
        {
            if (tuple1.Errors.Count <= tuple2.Errors.Count && tuple1.Errors.Count <= tuple3.Errors.Count)
                return tuple1;

            if (tuple2.Errors.Count <= tuple1.Errors.Count && tuple2.Errors.Count <= tuple3.Errors.Count)
                return tuple2;

            return tuple3;
        }


        private Token GetToken(int currentPosition)
        {
            return tokens[currentPosition];
        }


        private bool IsAtEnd(int currentPosition)
        {
            return currentPosition >= tokens.Count;
        }

        private bool IsContainToken(TokenType tokenType, int pos)
        {
            for(int i = pos; i<tokens.Count; i++)
            {
                if (tokens[i].Type == tokenType)
                    return true;
            }
            return false;
        }

        private void AddError(int currentPosition, TokenType expectedTokenType, ErrorType errorType, List<Error> errors)
        {
           

            errors.Add(new Error(
                CreateErrorMessage(currentPosition, expectedTokenType, errorType),
                GetToken(currentPosition).Line,
                GetToken(currentPosition).EndColumn
            ));
        }

        private string CreateErrorMessage(int currentPosition, TokenType expectedTokenType, ErrorType errorType)
        {
            var actualToken = GetToken(currentPosition);

            if((actualToken.Type == TokenType.OPEN_BRACKET || actualToken.Type == TokenType.CLOSE_BRACKET || expectedTokenType == TokenType.CLOSE_BRACKET) 
                && (expectedTokenType != TokenType.END || expectedTokenType == TokenType.END && errorType == ErrorType.DELETE))
            {
                return "Нужно соблюсти баланс скобок";
            }

            return errorType switch
            {
                ErrorType.DELETE or ErrorType.DELETE_END =>
                    $"{errorType.GetDescription()}: '{actualToken.Value}'",

                ErrorType.PUSH =>
                    $"{errorType.GetDescription()}: '{GetTokenTypeDescription(expectedTokenType)}'",

                ErrorType.REPLACE =>
                    $"Ожидалось: '{GetTokenTypeDescription(expectedTokenType)}' Фактически: '{actualToken.Value}'",

                _ => string.Empty
            };
        }

        private string GetTokenTypeDescription(TokenType type)
        {
            return type switch
            {
                TokenType.LAMBDA => "lambda",
                TokenType.NUMBER => "число",
                TokenType.IDENTIFIER => "идентификатор",
                TokenType.OPEN_BRACKET => "(",
                TokenType.CLOSE_BRACKET => ")",
                TokenType.EQUAL => "=",
                TokenType.PLUS_OPERATION => "+ или -",
                TokenType.MULTIPLICATION_OPERATION => "* или /",
                TokenType.COLON => ":",
                TokenType.COMMA => ",",
                TokenType.END => ";",
                TokenType.INVALID => "невалидный токен",
                TokenType.WHITESPACE => "пробел",
                _ => type.ToString()
            };
        }
    }
}
