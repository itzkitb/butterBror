using System.Net;
using System.Text;

namespace butterBror
{
    namespace Utils
    {
        public partial class ConsoleServer
        {
            private static readonly Dictionary<string, StreamWriter> consoleWriters = new Dictionary<string, StreamWriter>();
            private static readonly Dictionary<string, string> clientTokens = new Dictionary<string, string>();
            private static readonly Dictionary<string, bool> clientPings = new Dictionary<string, bool>();
            private static readonly Dictionary<string, bool> clientConnected = new Dictionary<string, bool>();
            private static Dictionary<string, bool> registeredClients = new Dictionary<string, bool>();
            public static Dictionary<string, string> passwords = new Dictionary<string, string>();
            public static async Task Main()
            {
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:5051/");
                listener.Start();
                ConsoleUtil.LOG($"Консоль сервера расположена по следующему адресу: http://localhost:5051/");
                var tasks = new List<Task>();
                Task task = new(async () =>
                {
                    while (true)
                    {
                        passwords["ItzKITb"] = "206613";
                        HttpListenerContext context = await listener.GetContextAsync();
                        string clientToken;
                        if (!clientTokens.TryGetValue(context.Request.UserHostName, out clientToken))
                        {
                            clientToken = GenerateToken();
                            clientTokens[context.Request.UserHostName] = clientToken;
                            ConsoleUtil.LOG($"[SC] {context.Request.UserHostName} зарегистрирован как {clientToken}");
                            registeredClients[clientToken] = true;
                        }
                        else
                        {
                            clientToken = clientTokens[context.Request.UserHostName];
                        }
                        clientPings[clientToken] = true;
                        clientConnected[clientToken] = true;
                        tasks.Add(HandleRequest(context, clientToken));
                        tasks = tasks.Where(t => !t.IsCompleted).ToList();
                    }
                });
                task.Start();
            }
            static async Task HandleRequest(HttpListenerContext context, string clientToken)
            {
                string absolutePath = context.Request.Url.AbsolutePath;
                if (absolutePath == "/")
                {
                    if (registeredClients[clientToken])
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes(HtmlPage);
                        context.Response.ContentLength64 = buffer.Length;
                        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Close();
                        ConsoleUtil.LOG($"[SC] {clientToken} открыл \"{absolutePath}\"");
                    }
                    else
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes(LoginPage);
                        context.Response.ContentLength64 = buffer.Length;
                        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Close();
                        ConsoleUtil.LOG($"[SC] {clientToken} открыл \"{absolutePath}\"");
                    }
                }
                else if (absolutePath.StartsWith("/console/") && registeredClients[clientToken])
                {
                    string consoleName = absolutePath.Substring("/console/".Length);
                    await HandleConsoleConnection(context, consoleName, clientToken);
                }
                else if (absolutePath == "/ping" && false)
                {
                    clientPings[clientToken] = true;
                    clientConnected[clientToken] = true;
                    Console.WriteLine($" | SC: [{DateTime.Now}] {clientToken} pinged!");
                    await HandleConsoleConnection(context, "ping", clientToken);
                }
                else if (absolutePath == "/pong" && false)
                {
                    clientPings[clientToken] = true;
                    Console.WriteLine($" | SC: [{DateTime.Now}] {clientToken} ponged!");
                }
                else if (absolutePath.StartsWith("/login/"))
                {
                    var loginData = absolutePath.Substring("/login/".Length).Split("/");
                    if (loginData.Length > 1)
                    {
                        if (passwords.ContainsKey(loginData[0].Replace("name=", "")))
                        {
                            if (passwords[loginData[0].Replace("name=", "")] == loginData[1].Replace("pass=", ""))
                            {
                                registeredClients[clientToken] = true;
                                Console.WriteLine(" | Login succeful!");
                            }
                            else
                            {
                                Console.WriteLine(" | Login error: wrong password");
                            }
                        }
                        else
                        {
                            Console.WriteLine(" | Login error: wrong username");
                        }
                    }
                    else
                    {
                        Console.WriteLine($" | Wrong login data: {string.Join(", ", loginData)}");
                    }
                }
                else if (absolutePath.StartsWith("/botControl/") && registeredClients[clientToken])
                {
                    string action = absolutePath.Substring("/botControl/".Length).Replace("/", "");
                    ConsoleUtil.LOG($"[SC] {clientToken} выполнил действие: \"{action}\"");
                    if (action.Contains("turnOff"))
                    {
                        await CommandUtil.ChangeNicknameColorAsync(TwitchLib.Client.Enums.ChatColorPresets.DodgerBlue);
                        Bot.client.SendMessage(Bot.BotNick, "Zevlo Выключение...");
                        Bot.client.SendMessage("itzkitb", "Zevlo Выключение...");
                        await Task.Delay(2000);
                        ConsoleUtil.LOG($"Бот выключен!", ConsoleColor.Black, ConsoleColor.Cyan);
                        BotEngine.isTwitchReady = false;
                        Bot.client.Disconnect();
                        await Bot.discordClient.DisposeAsync();
                        DataManagers.UsersData.ClearData();
                        await Task.Delay(5000);
                        Environment.Exit(0);
                    }
                }
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            private static string GenerateToken()
            {
                return Guid.NewGuid().ToString();
            }
            private static async Task HandleConsoleConnection(HttpListenerContext context, string consoleName, string clientToken)
            {
                StreamWriter writer = null;
                try
                {
                    context.Response.ContentType = "text/event-stream";
                    context.Response.Headers.Add("Cache-Control", "no-cache");
                    context.Response.Headers.Add("Connection", "keep-alive");

                    writer = new StreamWriter(context.Response.OutputStream);
                    consoleWriters[clientToken] = writer;
                    if (consoleName != "ping")
                    {
                        await WriteAsync($"data: console:events -- {DateTime.Now} | Подключено! (Токен клиента: {clientToken})\n\n", writer);
                        await WriteAsync($"data: console:discord -- {DateTime.Now} | Подключено! (Токен клиента: {clientToken})\n\n", writer);
                        await WriteAsync($"data: console:data -- {DateTime.Now} | Подключено! (Токен клиента: {clientToken})\n\n", writer);
                        await WriteAsync($"data: console:errors -- {DateTime.Now} | Подключено! (Токен клиента: {clientToken})\n\n", writer);
                        await WriteAsync($"data: console:info -- {DateTime.Now} | Подключено! (Токен клиента: {clientToken})\n\n", writer);
                        await WriteAsync($"data: console:cafus -- {DateTime.Now} | Подключено! (Токен клиента: {clientToken})\n\n", writer);
                        await WriteAsync($"data: console:commands -- {DateTime.Now} | Подключено! (Токен клиента: {clientToken})\n\n", writer);
                        await WriteAsync($"data: console:files -- {DateTime.Now} | Подключено! (Токен клиента: {clientToken})\n\n", writer);
                    }

                    bool isConnected = true;

                    while (isConnected)
                    {
                        if (consoleName == "ping")
                        {
                            await Task.Delay(5000);
                            clientPings[clientToken] = false;
                            writer = new StreamWriter(context.Response.OutputStream);
                            await WriteAsync("data: ping\n\n", writer);
                            Console.WriteLine($" | SC: [{DateTime.Now}] Ping ({clientToken})...");
                            await Task.Delay(1000);
                            if (!clientPings[clientToken])
                            {
                                isConnected = false;
                                clientConnected[clientToken] = false;
                                clientPings.Remove(clientToken);
                                clientTokens.Remove(context.Request.UserHostAddress);
                                Console.WriteLine($" | SC: [{DateTime.Now}] Клиент ({clientToken}) не ответил на пинг и был отключен");
                                break;
                            }
                            Console.WriteLine($" | SC: [{DateTime.Now}] Pinged!");
                        }
                        else
                        {
                            await Task.Delay(1000);
                            if (!clientConnected[clientToken])
                            {
                                isConnected = false;
                                Console.WriteLine($" | SC: [{DateTime.Now}] Клиент ({clientToken}) был отключен от {consoleName}");
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ConsoleUtil.LOG($"[SC] Ошибка: {ex.Message}", ConsoleColor.Red);
                }
                finally
                {
                    // Закрытие потока и удаление из словаря при любом исходе
                    writer?.Close();
                    consoleWriters.Remove(clientToken + "/" + consoleName);
                    ConsoleUtil.LOG($"[SC] Соединение с {clientToken} разорвано");
                }
            }
            public static async Task SendConsoleMessage(string consoleName, string message)
            {
                foreach (var token in clientTokens)
                {
                    try
                    {
                        if (consoleWriters.TryGetValue(token.Value, out StreamWriter writer))
                        {
                            await WriteAsync($"data: console:{consoleName} -- {DateTime.Now} | {message}\n\n", writer);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            public static async Task WriteAsync(string data, StreamWriter writer)
            {
                await writer.WriteAsync(data);
                await writer.FlushAsync();
            }
            private static string HtmlPage => @"
<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta http-equiv='X-UA-Compatible' content='IE=edge'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta charset=""UTF-8"">
    <title>Консоль</title>
    <script>
document.addEventListener('DOMContentLoaded', function() {
    var evtSource = new EventSource('/console/events');
    evtSource.onmessage = function(e) {
        var data = e.data.slice(7); // Убираем ""data: "" из строки
        var parts = data.split(' -- '); // Разделяем на части по "" | ""
        if (parts.length > 1) {
            var consolePart = parts[0].split(' ')[0]; // Получаем первую часть до пробела, которая должна быть ""console:{consoleName}""
            var consoleType = consolePart.split(':')[1]; // Извлекаем {consoleName}
            var message = parts[1]; // Сообщение - это вторая часть после "" | ""
            
            var newElement = document.createElement(""div"");
            newElement.textContent = message;

            var addLine = true;
            var console2;
            switch(consoleType) {
                case 'discord':
                    console2 = document.getElementById('discordConsole');
                    break;
                case 'errors':
                    console2 = document.getElementById('errorsConsole');
                    break;
                case 'commands':
                    console2 = document.getElementById('commandsConsole');
                    break;
                case 'data':
                    console2 = document.getElementById('dataConsole');
                    console2.textContent = message;
                    addLine = false;
                    break;
                case 'info':
                    console2 = document.getElementById('infoConsole');
                    break;
                case 'cafus':
                    console2 = document.getElementById('cafusConsole');
                    break;
                case 'events':
                    console2 = document.getElementById('twitchConsole');
                    break;
                case 'files':
                    console2 = document.getElementById('filesConsole');
                    break;
                default:
                    console2 = null;
            }
        }

        if (console2) {
            if (addLine) {
                if (console2.firstChild) {
                    console2.insertBefore(newElement, console2.firstChild);
                } else {
                    console2.appendChild(newElement);
                }
                if (console2.childNodes.length > 1000) {
                    console2.removeChild(console2.lastChild);
                }
            }
        } else {
            console.log('Не найден элемент консоли для типа: ' + consoleType);
        }
    };

    evtSource.onerror = function() {
        var newElement = document.createElement(""div"");
        newElement.textContent = 'Соединение потеряно. Пожалуйста, перезагрузите страницу.';
        document.querySelectorAll('.console').forEach(function(console2) {
            console2.insertBefore(newElement.cloneNode(true), console2.firstChild);
        });
    };
});
</script>

<style>
    .console {
        margin-right: 20%;
        margin-left: 20%;
        border-radius: 25px;
        height: 400px;
        font-family: 'Lucida Console';
        background: #808080;
        overflow-y: auto;
        color: #ffffff;
        font-size: 12px;
        padding-top: 30px;
        padding-bottom: 30px;
        padding-left: 30px;
        padding-right: 30px;
        text-align: left;
        margin-bottom: 10px;
    }
    .dataConsole {
        margin-right: 20%;
        margin-left: 20%;
        border-radius: 25px;
        font-family: 'Lucida Console';
        background: #303030;
        overflow-y: auto;
        color: #ffffff;
        font-size: 12px;
        padding-top: 20px;
        padding-bottom: 20px;
        padding-left: 20px;
        padding-right: 20px;
        text-align: left;
        margin-bottom: 10px;
    }
    .consoleTitle {
        margin-right: 20%;
        margin-left: 20%;
        margin-bottom: 10px;
        font-family: 'Lucida Console';
        color: #ffffff;
        font-size: 12px;
        text-align: left;
    }
    .tableConsoleTitle {
        margin-bottom: 10px;
        font-family: 'Lucida Console';
        color: #ffffff;
        font-size: 12px;
        text-align: left;
    }
    table {
        width: 60%;
        border-collapse: collapse;
        margin-right: 20%;
        margin-left: 20%;
    }
    .tableConsole {
        margin: 0;
        border-radius: 25px;
        height: 400px;
        font-family: 'Lucida Console';
        background: #808080;
        overflow-y: auto;
        color: #ffffff;
        font-size: 12px;
        padding-top: 30px;
        padding-bottom: 30px;
        padding-left: 30px;
        padding-right: 30px;
        text-align: left;
    }
    td {
        vertical-align: middle;
        text-align: center;
        width: 50%;
    }
    body {
        background-color: #202125;
        color: #ffffff;
        text-align: center;
        font-family: 'Arial';
    }
    table, td {
        border: 10px solid transparent;
        border-collapse: collapse;
    }
    button {
        background-color: #007BFF;
        color: white;
        border: none;
        border-radius: 15px;
        padding: 10px 20px;
        cursor: pointer;
        font-size: 16px;
    }
    button:hover {
        background-color: #0056b3;
    }
</style>
</head>
<body>
    <h1>Bot Console</h1>
    <div class=""consoleTitle"">Twitch msg's Console</div>
    <div class=""console"" id=""twitchConsole""></div>
    <div class=""consoleTitle"">Discord Console</div>
    <div class=""console"" id=""discordConsole""></div>
    <table>
        <tbody>
            <tr>
                <td>
                    <div class=""tableConsoleTitle"">Errors Console</div>
                    <div class=""tableConsole"" id=""errorsConsole""></div>
                </td>
                <td>
                    <div class=""tableConsoleTitle"">Commands Console</div>
                    <div class=""tableConsole"" id=""commandsConsole""></div>
                </td>
            </tr>
        </tbody>
        <tbody>
            <tr>
                <td>
                    <div class=""tableConsoleTitle"">Info Console</div>
                    <div class=""tableConsole"" id=""infoConsole""></div>
                </td>
                <td>
                    <div class=""tableConsoleTitle"">CAFUS Console</div>
                    <div class=""tableConsole"" id=""cafusConsole""></div>
                </td>
            </tr>
        </tbody>
    </table>
    <div class=""consoleTitle"">Data Console</div>
    <div class=""dataConsole"" id=""dataConsole""></div>
    <div class=""consoleTitle"">Files Console</div>
    <div class=""console"" id=""filesConsole""></div>
    <div> </div>
    <button id=""turnOffButton"">Выключить Бота</button>
    <div> </div>
    <div> </div>
    <div>butterBror console</div>
    <div>Copyright (C) ItzKITb</div>
    <script>
        document.getElementById('turnOffButton').addEventListener('click', function() {
            var xhr = new XMLHttpRequest();
            xhr.open(""POST"", ""/botControl/turnOff"", true);
            xhr.onreadystatechange = function() {
                if (xhr.readyState == 4 && xhr.status == 200) {
                    console.log('Запрос отправлен');
                }
            };
            xhr.send();
        });
    </script>
</body>
</html>
";
            private static string LoginPage => @"<!DOCTYPE html>
<html lang=""ru"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Страница Входа</title>
<style>
  body {
    background-color: #202125;
    color: white;
    font-family: Arial, sans-serif;
    display: flex;
    justify-content: center;
    align-items: center;
    height: 100vh;
    margin: 0;
  }
  form {
    display: flex;
    flex-direction: column;
    width: 300px;
  }
  input {
    margin-bottom: 10px;
    border-radius: 15px;
    border: none;
    padding: 10px;
  }
  button {
    background-color: #007BFF;
    color: white;
    border: none;
    border-radius: 15px;
    padding: 10px;
    cursor: pointer;
  }
  button:hover {
    background-color: #0056b3;
  }
</style>
</head>
<body>
<form id=""loginForm"">
  <input type=""text"" id=""username"" placeholder=""Имя пользователя"" required>
  <input type=""password"" id=""password"" placeholder=""Пароль"" required>
  <button type=""submit"">Отправить</button>
</form>

<script>
  document.getElementById('loginForm').addEventListener('submit', function(event) {
    event.preventDefault();
    var username = document.getElementById('username').value;
    var password = document.getElementById('password').value;
    fetch('/login/name=' + encodeURIComponent(username) + '/pass=' + encodeURIComponent(password))
      .then(response => response.text())
      .then(data => {
        window.location.href = data;
        location.reload(); // Добавлено обновление страницы
      })
      .catch(error => console.error('Ошибка:', error));
  });
</script>
</body>
</html>
";
        }
    }
}
