# OK.Bitter

OK.Bitter is developed to listen Binance crypto currency symbol websockets and when price is changed that less/more than users' specified treshold, send message via Telegram Bot or call via IFTTT Voice Call.

## The Structure

Basically, there is an api named "OK.Bitter.Api". The api has bot commands to manage Telegram Bot messages that user sent with webhook and has two hosted service that running in background.

First hosted service SymbolHostedService runs every 30 minutes and updates symbols in database to get new symbols immediately.
Second hosted service SocketHostedService listens Binance Websockets and decide notify or call user or do nothing.

## The Scenario

1. Api starts, bot webhook is up and hosted services are running in background.
1. User joins the Telegram Bot that your bot created using Telegram BotFather from Telegram Mobile App.
2. User sends command '/auth password' to bot. If password is 'normal!123' then user will be created as Normal User, if password is 'admin!123' then user will be created as Admin User. Otherwise user cannot be notified for any price change.
3. User sends command '/subscriptions set ETH|BTC 1.0' to take message for every 1.0 percent price change for ETH coin.
4. When user sends the subscribe command to bot, Telegram sends the message to our api and our api receives the request. Api stores the subscription in database and tells 'Hey, the user wants to take a notification when the ETH|BTC coin's every 1.0 percent changes' to socket hosted service.
5. When the subscription changes are performed on Binance market. Our hosted service takes it from websocket and sends a message the user via Telegram Bot.
6. User sends command '/alerts set BTC|USDT less 3500 greater 4000' to take a voice call for the specified bounds.
7. When user sends the alert command to bot, Telegram sends the message to our Webhook api and our api receives the request. Api stores the alert in database and tells 'Hey, the user wants to take a alert when the BTC|USDT coin's price is not between $3500 and $4000 prices' to socket hosted service.
8. When the BTC|USDT price is not between $3500 and $4000, Our hosted service takes it from websocket and sends a request to IFTTT api to make a voice call that contains 'Hey, BTC price is $4001.'. 

## How to Use

1. Create a Telegram Bot using Telegram BotFather. The BotFather give you a bot token. Keep it and write to ServiceConfigurations.TelegramService.BotToken property in 'src\OK.Bitter.Api\appsettings.json' file. For more information about creating Telegram Bot, go to official telegram documentation site.
2. Create a MongoDB database and copy your database connection string. Write to MongoConfigurations.ConnectionString property in 'src\OK.Bitter.Api\appsettings.json' file.
3. Open the CMD or Bash. Change directory to 'src\OK.Bitter.Api'.
5. Run the `dotnet run` command. And now your service listens sockets and notifies you via the Telegram Bot.
6. When you deploy the api to your server with a domain, you should update your Telegram Bot webhook endpoint as 'your_domain_com/api/bot/update' using Telegram BotFather commands.
6. Enjoy!