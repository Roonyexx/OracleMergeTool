using System;
using PlSqlMergeTool.BLL.LexicalAnalysis;
using PlSqlMergeTool.BLL.Models;


string testSql = @"
SELECT id, v_user$name
FROM users -- таблица пользователей
WHERE age >= 18 AND status = 'Активен''Да';
/* Многострочный
   комментарий */
v_total := 100 + 50;
";

var scanner = new Scanner(testSql);
PlSqlToken? token;

Console.WriteLine($"{"ТИП",-20} | {"ТЕКСТ",-25} | {"СТРОКА",-7} | {"ПОЗИЦИЯ",-8} | {"ДЛИНА"}");
Console.WriteLine(new string('-', 80));

while ((token = scanner.GetNextToken()) != null)
{
    string safeText = token.Text.Replace("\n", "\\n").Replace("\r", "");
    Console.WriteLine($"{token.Type,-20} | '{safeText,-23}' | {token.Line,-7} | {token.Offset,-8} | {token.Length}");
}

Console.WriteLine(new string('-', 80));
