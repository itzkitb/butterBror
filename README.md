[![.NET CI](https://github.com/itzkitb/butterBror/actions/workflows/ci.yml/badge.svg)](https://github.com/itzkitb/butterBror/actions/workflows/ci.yml)

<img 
    src="https://images.itzkitb.lol/butterbror/github_logo.png" 
    alt="logo"
/>

ButterBror is a powerful multipurpose chat bot designed for entertainment and interaction in chats. It works simultaneously on Discord, Twitch, and Telegram, offering a wide range of fun and useful commands.

## ✨ Features and Commands

The bot offers numerous commands for various situations:

### 🎮 Entertainment and Games
* `_8ball` - Magic ball for answering questions
* `_coinflip` - Flip a coin
* `_roulette` - Roulette
* `_rr` - Russian roulette
* `_frog` - Frogs mini-game
* `_emotes` - View emotes (7TV)
* `_ccokie` - Daily cookie

### 💰 Economy
* `_balance` - Check balance
* `_hourly`, `_daily`, `_weekly`, `_monthly`, `_yearly` - Timed rewards

### 🛠 Utilities
* `_afk` / `_resumeafk` - "Away from keyboard" mode
* `_weather` - Weather forecast
* `_calc` - Calculator
* `_currency` - Currency converter
* `_ping` - Check latency & status

### 💬 Chat and Interaction
* `_firstline` / `_lastline` - First/last line in chat
* `_firstgloballine` / `_lastgloballine` - Global first/last lines
* `_ai` - AI assistant
* `_js` - Execute JavaScript code
* `_bot` - Bot information
* `_lang <en/ru>` - Set language for commands

### ❓ Help
* `_help` - List of all commands

## 🌟 Supported Platforms

The bot works simultaneously on three platforms:
- **Discord**
- **Twitch**
- **Telegram**

## 🛠 Technologies

* **Language:** C# (.NET)
* **Database:** SQLite, JSON
* **Data Storage:** `%AppData%\SillyApps\ButterBror`
* **Main Libraries:**
  - `Discord.Net` - Discord integration
  - `Telegram.Bot` - Telegram integration
  - `TwitchLib` - Twitch integration
  - `SevenTV-lib` - 7TV emotes integration
  - `Jint` / `Microsoft.CodeAnalysis.CSharp.Scripting` - Script execution
  - `DankDB` - JSON database

## 📦 Installation and Setup

### Requirements
- Installed [.NET Runtime](https://dotnet.microsoft.com/download)

### Installation Steps:

1. **Download the latest release** from [releases page](https://github.com/your-username/ButterBror/releases)

2. **Run the application** `Host.exe`
   - On first run, it will automatically create the folder `%AppData%\SillyApps\ButterBror`
   - The bot will automatically download the latest version and dependencies

3. **Configure settings**:
   - After first run, a `settings.xml` file will be created in `%AppData%\SillyApps\ButterBror`
   - Fill the file with necessary tokens and settings (see Configuration section)

4. **Restart the bot** to apply settings

## ⚙️ Configuration

The `settings.xml` file should contain the following settings:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Settings>
    <!-- OpenRouter API tokens for AI features -->
    <open_router_tokens>
        <item>first_api_key</item>
        <item>second_api_key</item>
    </open_router_tokens>
    
    <!-- Discord Bot Token from https://discord.com/developers/applications -->
    <discord_token>YOUR_DISCORD_BOT_TOKEN</discord_token>
    
    <!-- 7TV Token (Your bot's Twitch nickname for 7TV integration) -->
    <seventv_token>YOUR_BOT_TWITCH_NICKNAME</seventv_token>
    
    <!-- Command prefix -->
    <prefix>!</prefix>
    
    <!-- Economy settings -->
    <taxes_cost>0.0069</taxes_cost>
    <currency_mentioner_payment>2</currency_mentioner_payment>
    <currency_mentioned_payment>8</currency_mentioned_payment>
    
    <!-- Dashboard password (SHA512 hash) -->
    <dashboard_password>6FF8E2CF58249F757ECEE669C6CB015A1C1F44552442B364C8A388B0BDB1322A7AF6B67678D9206378D8969FFEC48263C9AB3167D222C80486FC848099535568</dashboard_password>
    
    <!-- Bot name (Your bot's Twitch nickname) -->
    <bot_name>YOUR_BOT_TWITCH_NICKNAME</bot_name>
    
    <!-- GitHub token for repository interactions (create at https://github.com/settings/tokens with public_repo scope) -->
    <github_token>YOUR_GITHUB_TOKEN</github_token>
    
    <!-- Telegram Bot Token from @BotFather -->
    <telegram_token>YOUR_TELEGRAM_BOT_TOKEN</telegram_token>
    
    <!-- Twitch Configuration -->
    <twitch_user_id>YOUR_TWITCH_ACCOUNT_ID</twitch_user_id>
    <twitch_client_id>YOUR_TWITCH_CLIENT_ID</twitch_client_id>
    <twitch_secret_token>YOUR_TWITCH_SECRET_TOKEN</twitch_secret_token>
    
    <!-- Twitch Channel Lists -->
    <twitch_connect_message_channels>
        <item>CHANNEL_ID_FOR_CONNECT_MESSAGES_ANNOUNCE</item>
        <item>CHANNEL_ID_FOR_CONNECT_MESSAGES_ANNOUNCE</item>
    </twitch_connect_message_channels>
    
    <twitch_reconnect_message_channels>
        <item>CHANNEL_ID_FOR_RECONNECT_MESSAGES_ANNOUNCE</item>
        <item>CHANNEL_ID_FOR_RECONNECT_MESSAGES_ANNOUNCE</item>
    </twitch_reconnect_message_channels>
    
    <twitch_version_message_channels>
        <item>CHANNEL_ID_FOR_VERSION_MESSAGES_ANNOUNCE</item>
        <item>CHANNEL_ID_FOR_VERSION_MESSAGES_ANNOUNCE</item>
    </twitch_version_message_channels>
    
    <twitch_currency_random_event>
        <item>CHANNEL_ID_FOR_CURRENCY_EVENTS_ANNOUNCE</item>
        <item>CHANNEL_ID_FOR_CURRENCY_EVENTS_ANNOUNCE</item>
    </twitch_currency_random_event>
    
    <twitch_taxes_event>
        <item>CHANNEL_ID_FOR_TAXES_EVENTS_ANNOUNCE</item>
        <item>CHANNEL_ID_FOR_TAXES_EVENTS_ANNOUNCE</item>
    </twitch_taxes_event>
    
    <twitch_connect_channels>
        <item>CHANNEL_ID_TO_CONNECT</item>
        <item>CHANNEL_ID_TO_CONNECT</item>
    </twitch_connect_channels>
    
    <twitch_dev_channels>
        <item>CHANNEL_ID_FOR_DEVELOPMENT_ANNOUNCE</item>
        <item>CHANNEL_ID_FOR_DEVELOPMENT_ANNOUNCE</item>
    </twitch_dev_channels>
</Settings>
```

**How to get tokens:**
- **Discord:** [Discord Developer Portal](https://discord.com/developers/applications)
- **Telegram:** [@BotFather](https://t.me/BotFather)
- **Twitch:** [Twitch Developer Console](https://dev.twitch.tv/console)
- **OpenRouter:** [OpenRouter API Keys](https://openrouter.ai/keys)
- **GitHub:** [GitHub Personal Access Tokens](https://github.com/settings/tokens)

## 🤖 Usage

### Add the bot to your platforms:

- **Twitch:** Visit [https://twitch.tv/butterbror](https://twitch.tv/butterbror)
- **Telegram:** [@butterBror_bot](https://t.me/butterBror_bot)
- **Discord:** [Invite bot to server](https://discord.com/oauth2/authorize?client_id=1257568846500462593&permissions=8&response_type=code&redirect_uri=https%3A%2F%2Fitzkitb.lol%2Fbot_thanks&integration_type=0&scope=messages.read+bot+applications.commands)

### Usage examples:
```
User: _help
ButterBror: 👋 | https://itzkitb.lol/bot 

User: _weather Moscow
ButterBror: ☀️ | Ut, Khyber Pakhtunkhwa, Pakistan • 22.5°C Feels like: 20.2°C • 7.4 m/s • Clear Sky ☀️ • Pressure: 1016 hPa • UV: 4.2 • Humidity: 23% • Visibility: 24 km

User: _coinflip
ButterBror: 🪙 | Tails!
```

## 🗺 Roadmap

- [ ] **Moderation improvements** - More tools for chat management
- [ ] **Code refactoring** - Improved architecture and performance
- [ ] **New entertainment commands** - Continuously expanding functionality
- [ ] **Economic system development** - Improved in-game economy

## 👥 Contributing

We welcome contributions to the project! You can:

- [Report bugs](https://forms.gle/PY39uP9jy122VfZo6) and [suggest](https://github.com/itzkitb/butterBror/issues) new features
- Create pull requests with improvements
- Help test new versions

**Contact the developer:**
- Twitch: [https://twitch.tv/itzkitb](https://twitch.tv/itzkitb)
- Email: [itzkitb@gmail.com](mailto:itzkitb@gmail.com)

## 📄 License

This project is distributed under the MIT License. For more details, see the `LICENSE` file.

## 📞 Contacts

- **Author:** itzkitb
- **Twitch:** [https://twitch.tv/itzkitb](https://twitch.tv/itzkitb)
- **Email:** [itzkitb@gmail.com](mailto:itzkitb@gmail.com)

---

*SillyApps :P*