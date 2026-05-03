using PlSqlMergeTool.BLL.Models;

namespace PlSqlMergeTool.BLL.LexicalAnalysis;

public class Scaner(string input)
{
    private readonly string _input = input;
    private int _position = 0;
    private int _currentLine = 1;

    private void SkipWhitespace()
    {
        while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
        {
            if (_input[_position] == '\n') _currentLine++;
            _position++;
        }
    }
    public PlSqlToken? GetNextToken()
    {
        SkipWhitespace();
        if (_position >= _input.Length) return null;

        int startOffset = _position;

        if(char.IsLetter(_input[_position]) || _input[_position] == '_')
        {
            while (_position < _input.Length && (char.IsLetterOrDigit(_input[_position]) || _input[_position] == '_'))
            {
                _position++;
            }
        }
        else if (_input[_position] == '-')
        {
            _position++;
            if (_position < _input.Length && _input[_position] == '-')
            {
                while (_position < _input.Length && _input[_position] != '\n')
                {
                    _position++;
                }
            }
        }
        else if (_input[_position] == '/')
        {
            _position++;
            if (_position < _input.Length && _input[_position] == '*')
            {
                _position++;
                while (_position < _input.Length - 1)
                {
                    if (_input[_position] == '*' && _input[_position + 1] == '/')
                    {
                        _position += 2;
                        break;
                    }
                    if (_input[_position] == '\n') _currentLine++;
                    _position++;
                }
                if (_position == _input.Length - 1) _position++; 
            }
        }
        else
        {
            _position++;
        }

        int tokenLength = _position - startOffset;
        string token = _input[startOffset.._position];

        return new PlSqlToken
        {
            Text = token,
            Line = _currentLine,
            Offset = startOffset,
            Length = tokenLength
        };
    }
}