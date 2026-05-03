using System;
using PlSqlMergeTool.BLL.Models;

namespace PlSqlMergeTool.BLL.LexicalAnalysis;

public class Scanner(string input)
{
    private readonly string _input = input;
    private int _position = 0;
    private int _currentLine = 1;
    private static readonly string[] ComplexOperators = 
    [
        ":=", ">=", "<=", "!=", "<>", "^=", "~=", "||", "**", "=>", "<<", ">>"
    ];

    public PlSqlToken? GetNextToken()
    {
        if (_position >= _input.Length) return null;

        int startOffset = _position;
        int startLine = _currentLine;
        char currentChar = _input[_position];
        // пробелы и табы, переносы строк
        if (char.IsWhiteSpace(currentChar))
        {
            while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            {
                if (_input[_position] == '\n') _currentLine++;
                _position++;
            }
            return CreateToken(startOffset, startLine, TokenType.Whitespace);
        }
        // однострочные комментарии
        if (currentChar == '-' && _position + 1 < _input.Length && _input[_position + 1] == '-')
        {
            _position += 2;
            while (_position < _input.Length && _input[_position] != '\n')
            {
                _position++;
            }
            return CreateToken(startOffset, startLine, TokenType.SingleLineComment);
        }
        // многострочные комментарии
        if (currentChar == '/' && _position + 1 < _input.Length && _input[_position + 1] == '*')
        {
            _position += 2;
            while (_position < _input.Length)
            {
                if (_input[_position] == '*' && _position + 1 < _input.Length && _input[_position + 1] == '/')
                {
                    _position += 2;
                    break;
                }
                if (_input[_position] == '\n') _currentLine++;
                _position++;
            }
            return CreateToken(startOffset, startLine, TokenType.MultiLineComment);
        }
        // строки
        if (currentChar == '\'')
        {
            _position++;
            while (_position < _input.Length)
            {
                if (_input[_position] == '\'')
                {
                    _position++;
                    if (_position < _input.Length && _input[_position] == '\'')
                        _position++;
                    else
                        break; 
                }
                else
                {
                    if (_input[_position] == '\n') _currentLine++;
                    _position++;
                }
            }
            return CreateToken(startOffset, startLine, TokenType.String);
        }

        // слова и идентификаторы
        if (char.IsLetter(currentChar) || currentChar == '_')
        {
            while (_position < _input.Length && 
                  (char.IsLetterOrDigit(_input[_position]) || _input[_position] == '_' || _input[_position] == '$' || _input[_position] == '#'))
            {
                _position++;
            }
            return CreateToken(startOffset, startLine, TokenType.Word);
        }

        // числа
        if (char.IsDigit(currentChar))
        {
            while (_position < _input.Length && (char.IsDigit(_input[_position]) || _input[_position] == '.'))
            {
                _position++;
            }
            return CreateToken(startOffset, startLine, TokenType.Number);
        }

        // операторы из нескольких символов
        var remainingSpan = _input.AsSpan(_position);
        foreach (var op in ComplexOperators)
        {
            if (remainingSpan.StartsWith(op))
            {
                _position += op.Length;
                return CreateToken(startOffset, startLine, TokenType.Operator);
            }
        }

        // разделители и одиночные операторы
        if ("();,.".Contains(currentChar))
        {
            _position++;
            return CreateToken(startOffset, startLine, TokenType.Delimiter);
        }

        if ("+-*/=<>:@%".Contains(currentChar))
        {
            _position++;
            return CreateToken(startOffset, startLine, TokenType.Operator);
        }

        // фоллбек для неизвестных символов
        _position++;
        return CreateToken(startOffset, startLine, TokenType.Unknown);
    }

    private PlSqlToken CreateToken(int startOffset, int startLine, TokenType type)
    {
        int length = _position - startOffset;
        return new PlSqlToken
        {
            Text = _input.Substring(startOffset, length),
            Line = startLine,
            Offset = startOffset,
            Length = length,
            Type = type
        };
    }
}