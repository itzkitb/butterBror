using System.Reflection;
using System.Text.RegularExpressions;

namespace bb
{
    public class Script
    {
        private Dictionary<string, object> _variables = new Dictionary<string, object>();
        private Dictionary<string, Function> _functions = new Dictionary<string, Function>();

        public object Execute(string code)
        {
            var statements = SplitIntoStatements(code);
            object result = null;
            foreach (var statement in statements)
            {
                result = ExecuteStatement(statement);
                if (result is ReturnValue returnValue)
                    return returnValue.Value;
            }
            return result;
        }

        private List<string> SplitIntoStatements(string code)
        {
            var statements = new List<string>();
            int start = 0;
            int braceLevel = 0;
            int bracketLevel = 0;
            int parenLevel = 0;
            bool inString = false;
            char stringChar = '\0';

            for (int i = 0; i < code.Length; i++)
            {
                char c = code[i];
                if (c == '"' || c == '\'')
                {
                    if (!inString)
                    {
                        inString = true;
                        stringChar = c;
                    }
                    else if (stringChar == c)
                    {
                        inString = false;
                    }
                }
                else if (!inString)
                {
                    if (c == '{') braceLevel++;
                    else if (c == '}')
                    {
                        braceLevel--;
                        // ДОБАВЛЕНО: Обработка закрытия функции
                        if (braceLevel == 0 && parenLevel == 0 && bracketLevel == 0)
                        {
                            int j = i + 1;
                            while (j < code.Length && char.IsWhiteSpace(code[j]))
                                j++;

                            if (j < code.Length && code[j] != ';')
                            {
                                // Если после } идет что-то кроме точки с запятой - это новое выражение
                                statements.Add(code.Substring(start, j - start).Trim());
                                start = j;
                            }
                        }
                    }
                    else if (c == '[') bracketLevel++;
                    else if (c == ']') bracketLevel--;
                    else if (c == '(') parenLevel++;
                    else if (c == ')') parenLevel--;

                    if (c == ';' && braceLevel == 0 && bracketLevel == 0 && parenLevel == 0)
                    {
                        statements.Add(code.Substring(start, i - start).Trim());
                        start = i + 1;
                    }
                }
            }

            if (start < code.Length)
            {
                string last = code.Substring(start).Trim();
                if (!string.IsNullOrEmpty(last))
                    statements.Add(last);
            }
            return statements;
        }

        private object ExecuteStatement(string statement)
        {
            if (string.IsNullOrWhiteSpace(statement))
                return null;

            Console.WriteLine(statement);

            // Обработка присваивания csVar
            if (Regex.IsMatch(statement, @"^csVar\s*\("))
            {
                var match = Regex.Match(statement, @"^csVar\s*\(""(.+?)""\)\s*=\s*(.+?)$");
                if (match.Success)
                {
                    string path = match.Groups[1].Value;
                    string expr = match.Groups[2].Value;
                    object value = EvaluateExpression(expr);
                    SetCsVar(path, value);
                    return value;
                }
            }

            // Обработка объявления переменной
            if (statement.StartsWith("var "))
            {
                var match = Regex.Match(statement, @"^var\s+(\w+)\s*=\s*(.+?)$");
                if (match.Success)
                {
                    string varName = match.Groups[1].Value;
                    string expr = match.Groups[2].Value;
                    object value = EvaluateExpression(expr);
                    _variables[varName] = value;
                    return value;
                }
            }

            // Вызов csExecute
            if (Regex.IsMatch(statement, @"^csExecute\s*\("))
            {
                var match = Regex.Match(statement, @"^csExecute\s*\(""(.+?)""\s*,\s*(\[.*\])\)$");
                if (match.Success)
                {
                    string path = match.Groups[1].Value;
                    string argsExpr = match.Groups[2].Value;
                    var args = EvaluateExpression(argsExpr) as List<object>;
                    return CallCsExecute(path, args ?? new List<object>());
                }
            }

            // Определение функции
            if (statement.StartsWith("func "))
            {
                DefineFunction(statement);
                return null;
            }

            // Возврат значения
            if (statement.StartsWith("return "))
            {
                string expr = statement.Substring(7).Trim();
                return new ReturnValue(EvaluateExpression(expr));
            }

            // Простое выражение
            return EvaluateExpression(statement);
        }

        private object EvaluateExpression(string expr)
        {
            expr = expr.Trim();

            // Обработка csVar (чтение)
            if (expr.StartsWith("csVar(") && expr.EndsWith(")"))
            {
                var match = Regex.Match(expr, @"^csVar\s*\(""(.+?)""\)$");
                if (match.Success)
                {
                    string path = match.Groups[1].Value;
                    return GetCsVar(path);
                }
            }

            // Обработка csExecute в выражениях
            if (expr.StartsWith("csExecute(") && expr.EndsWith(")"))
            {
                var match = Regex.Match(expr, @"^csExecute\s*\(""(.+?)""\s*,\s*(\[.*\])\)$");
                if (match.Success)
                {
                    string path = match.Groups[1].Value;
                    string argsExpr = match.Groups[2].Value;
                    var args = EvaluateExpression(argsExpr) as List<object>;
                    return CallCsExecute(path, args ?? new List<object>());
                }
            }

            if (Regex.IsMatch(expr, @"^(\w+)\s*\((.*)\)$"))
            {
                var funcCallMatch = Regex.Match(expr, @"^(\w+)\s*\((.*)\)$");
                if (funcCallMatch.Success)
                {
                    string name = funcCallMatch.Groups[1].Value;
                    string argsExpr = funcCallMatch.Groups[2].Value;

                    var args = new List<object>();
                    if (!string.IsNullOrWhiteSpace(argsExpr))
                    {
                        var argList = SplitArguments(argsExpr);
                        args = argList.Select(EvaluateExpression).ToList();
                    }

                    return CallFunction(name, args);
                }
            }

            // Строковые литералы
            if (expr.StartsWith("\"") && expr.EndsWith("\""))
                return expr[1..^1];

            // Массивы
            if (expr.StartsWith("[") && expr.EndsWith("]"))
            {
                string inner = expr[1..^1].Trim();
                if (string.IsNullOrEmpty(inner))
                    return new List<object>();

                var items = SplitArguments(inner);
                return items.Select(EvaluateExpression).ToList();
            }

            // Операторы
            foreach (var op in new[] { "^", "*/", "+-" })
            {
                int opIndex = FindOperator(expr, op);
                if (opIndex >= 0)
                {
                    string leftExpr = expr[..opIndex].Trim();
                    string rightExpr = expr[(opIndex + 1)..].Trim();
                    object leftVal = EvaluateExpression(leftExpr);
                    object rightVal = EvaluateExpression(rightExpr);
                    return ApplyOperator(expr[opIndex], leftVal, rightVal);
                }
            }

            // Переменные
            if (_variables.ContainsKey(expr))
                return _variables[expr];

            // Числа
            if (long.TryParse(expr, out long longVal))
                return longVal;
            if (double.TryParse(expr, out double doubleVal))
                return doubleVal;

            throw new Exception($"Unknown expression: {expr}");
        }

        private int FindOperator(string expr, string operators)
        {
            int level = 0;
            bool inString = false;

            for (int i = expr.Length - 1; i >= 0; i--)
            {
                char c = expr[i];
                if (c == '"' || c == '\'')
                    inString = !inString;

                else if (!inString)
                {
                    if (c == '(') level++;
                    else if (c == ')') level--;
                    else if (level == 0 && operators.Contains(c))
                        return i;
                }
            }
            return -1;
        }

        private object ApplyOperator(char op, object a, object b)
        {
            return op switch
            {
                '+' => (a is string || b is string)
                    ? $"{a}{b}"
                    : Convert.ToDouble(a) + Convert.ToDouble(b),
                '-' => Convert.ToDouble(a) - Convert.ToDouble(b),
                '*' => Convert.ToDouble(a) * Convert.ToDouble(b),
                '/' => Convert.ToDouble(a) / Convert.ToDouble(b),
                '^' => Math.Pow(Convert.ToDouble(a), Convert.ToDouble(b)),
                _ => throw new Exception($"Unknown operator: {op}")
            };
        }

        private object GetCsVar(string path)
        {
            var parts = path.Split('.');

            // Проверяем, что путь содержит как минимум namespace.класс.член
            if (parts.Length < 2)
                throw new Exception($"Invalid path format: {path}. Use 'Namespace.Class.Member'");

            // Собираем полное имя типа (все части кроме последней)
            string typeName = string.Join(".", parts.Take(parts.Length - 1));
            string memberName = parts.Last();

            // Ищем тип по полному имени (с namespace)
            Type type = Type.GetType(typeName);

            // Если не найден, ищем во всех загруженных сборках
            if (type == null)
            {
                type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == typeName);
            }

            if (type == null)
                throw new Exception($"Type '{typeName}' not found. Full path attempted: {typeName}");

            // Ищем статический член (поле или свойство)
            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
            var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static);

            if (field != null)
                return field.GetValue(null);
            if (prop != null)
                return prop.GetValue(null);

            throw new Exception($"Member '{memberName}' not found in type '{typeName}'");
        }

        private void SetCsVar(string path, object value)
        {
            var parts = path.Split('.');
            if (parts.Length < 2)
                throw new Exception($"Invalid path format: {path}. Use 'Namespace.Class.Member'");

            string typeName = string.Join(".", parts.Take(parts.Length - 1));
            string memberName = parts.Last();

            Type type = Type.GetType(typeName)
                        ?? AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.FullName == typeName);

            if (type == null)
                throw new Exception($"Type '{typeName}' not found");

            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
            var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static);

            if (field != null)
                field.SetValue(null, value);
            else if (prop != null)
                prop.SetValue(null, value);
            else
                throw new Exception($"Member '{memberName}' not found in type '{typeName}'");
        }

        private object CallCsExecute(string path, List<object> args)
        {
            var parts = path.Split('.');
            Type type = null;

            for (int i = 0; i < parts.Length; i++)
            {
                if (i == 0)
                {
                    type = Type.GetType(parts[0]) ??
                           AppDomain.CurrentDomain.GetAssemblies()
                               .SelectMany(a => a.GetTypes())
                               .FirstOrDefault(t => t.Name == parts[0]);
                    if (type == null) throw new Exception($"Type {parts[0]} not found");
                }
                else if (i == parts.Length - 1)
                {
                    var method = type.GetMethod(parts[i], BindingFlags.Public | BindingFlags.Static);
                    if (method == null) throw new Exception($"Method {parts[i]} not found");
                    return method.Invoke(null, args.ToArray());
                }
                else
                {
                    type = type.GetNestedType(parts[i], BindingFlags.Public)
                           ?? throw new Exception($"Nested type {parts[i]} not found");
                }
            }
            return null;
        }

        private void DefineFunction(string statement)
        {
            // Ищем начало функции (до открывающей скобки)
            var match = Regex.Match(statement, @"^func\s+(\w+)\s+(\w+)\s*\(");
            if (!match.Success)
                throw new Exception("Invalid function definition: " + statement);

            string returnType = match.Groups[1].Value;
            string name = match.Groups[2].Value;

            // Находим параметры и тело функции с учетом вложенных скобок
            int paramStart = match.Index + match.Length;
            int braceLevel = 0;
            bool inString = false;
            char stringChar = '\0';
            int paramEnd = -1;
            int bodyStart = -1;

            // ИСПРАВЛЕНО: Начинаем с уровня скобок = 1, так как мы уже внутри (
            braceLevel = 1;

            // Парсим параметры
            for (int i = paramStart; i < statement.Length; i++)
            {
                char c = statement[i];

                if (c == '"' || c == '\'')
                {
                    if (!inString)
                    {
                        inString = true;
                        stringChar = c;
                    }
                    else if (stringChar == c)
                    {
                        inString = false;
                    }
                }
                else if (!inString)
                {
                    if (c == '(') braceLevel++;
                    else if (c == ')') braceLevel--;

                    // ИСПРАВЛЕНО: Проверяем только уровень скобок
                    if (braceLevel == 0)
                    {
                        paramEnd = i;
                        break;
                    }
                }
            }

            if (paramEnd == -1)
                throw new Exception("Unclosed parameter list: " + statement);

            string paramList = statement.Substring(paramStart, paramEnd - paramStart);

            // Ищем тело функции
            for (int i = paramEnd + 1; i < statement.Length; i++)
            {
                if (statement[i] == '{')
                {
                    bodyStart = i + 1;
                    break;
                }
            }

            if (bodyStart == -1)
                throw new Exception("Function body not found: " + statement);

            // Парсим тело функции с учетом вложенных скобок
            braceLevel = 1;
            int bodyEnd = -1;
            inString = false;

            for (int i = bodyStart; i < statement.Length; i++)
            {
                char c = statement[i];

                if (c == '"' || c == '\'')
                {
                    inString = !inString;
                }
                else if (!inString)
                {
                    if (c == '{') braceLevel++;
                    else if (c == '}') braceLevel--;

                    if (braceLevel == 0)
                    {
                        bodyEnd = i;
                        break;
                    }
                }
            }

            if (bodyEnd == -1)
                throw new Exception("Unclosed function body: " + statement);

            string body = statement.Substring(bodyStart, bodyEnd - bodyStart).Trim();

            // Обработка параметров
            var parameters = new List<(string Type, string Name)>();
            if (!string.IsNullOrWhiteSpace(paramList))
            {
                var paramParts = SplitArguments(paramList);
                foreach (var p in paramParts)
                {
                    var parts = p.Trim().Split(' ');
                    if (parts.Length < 2)
                        throw new Exception($"Invalid parameter format: {p}");

                    parameters.Add((parts[0], parts[1]));
                }
            }

            _functions[name] = new Function
            {
                ReturnType = returnType,
                Parameters = parameters,
                Body = body
            };
        }

        private object CallFunction(string name, List<object> args)
        {
            if (!_functions.TryGetValue(name, out var func))
                throw new Exception($"Function {name} not defined");

            if (args.Count != func.Parameters.Count)
                throw new Exception("Argument count mismatch");

            // Создаем новый контекст
            var oldVars = _variables;
            _variables = new Dictionary<string, object>(oldVars);

            // Устанавливаем параметры
            for (int i = 0; i < args.Count; i++)
            {
                _variables[func.Parameters[i].Name] = args[i];
            }

            // Выполняем тело
            var result = Execute(func.Body);

            // Восстанавливаем контекст
            _variables = oldVars;
            return result;
        }

        private List<string> SplitArguments(string args)
        {
            var result = new List<string>();
            int start = 0;
            int level = 0;
            bool inString = false;

            for (int i = 0; i < args.Length; i++)
            {
                char c = args[i];
                if (c == '"' || c == '\'') inString = !inString;
                else if (!inString)
                {
                    if (c == '(' || c == '[' || c == '{') level++;
                    else if (c == ')' || c == ']' || c == '}') level--;
                    else if (c == ',' && level == 0)
                    {
                        result.Add(args[start..i].Trim());
                        start = i + 1;
                    }
                }
            }

            if (start < args.Length)
                result.Add(args[start..].Trim());

            return result;
        }

        // Вспомогательные классы
        private class Function
        {
            public string ReturnType;
            public List<(string Type, string Name)> Parameters;
            public string Body;
        }

        private class ReturnValue
        {
            public object Value;
            public ReturnValue(object value) => Value = value;
        }
    }
}
