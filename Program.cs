﻿using Discord.Interactions;
using Discord.Commands;
using BotTemplate.BotCore.Interactions.Buttons;
using BotTemplate.BotCore.TextCommands;
using BotTemplate.BotCore.DataAccess;
using Microsoft.EntityFrameworkCore;
using BotTemplate.BotCore.Repositories;
using BotTemplate.BotCore.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using BotTemplate.BotCore.Services;

// This is the main entry point of the application and uses top-level statements instead of a traditional Main method.
// Top-level statements allow for a more concise and streamlined setup, especially for simple applications.
// This is a personal preference. If you prefer, you can use a Main method to create an async context as shown in the Discord.NET guide.
//
// https://docs.discordnet.dev/guides/getting_started/first-bot.html#connecting-to-discord

// Set the current directory to the base directory of the application.
// This allows us to properly get the location of the .env file
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

// Configure the global logger for general application logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
    .WriteTo.Console()
    .CreateLogger();

// Configure a separate logger for database-related logging
var dbLogger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File("logs/database.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Build the configuration object to read from environment variables, .env files, and user secrets.
// IConfigurationRoot allows accessing configuration settings in a hierarchical manner.
IConfigurationRoot configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables(prefix: "TEMPLATE_") // Adds a specific prefix to variables to avoid conflicts. You can change the prefix to whatever you want.
    .AddDotNetEnv(Path.Combine(Directory.GetCurrentDirectory(), "../../../.env")) // You can change this path if needed.
    .AddUserSecrets<Program>()
    .Build();

// This is a flexible way to set up dependency injection, logging, and configuration for the bot.
// DI allows for components to be loosely coupled and easily swapped. This is needed for the Interaction Framework to be setup properly.
// Learn about how to scale your bot with DI https://docs.discordnet.dev/guides/dependency_injection/basics.html
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
string discordToken = builder.Configuration.GetValue<string>("DISCORD_TOKEN") ?? throw new InvalidOperationException("MISSING DISCORD_TOKEN; check .env file.");
string discordWebhookUrl = builder.Configuration.GetValue<string>("DISCORD_WEBHOOK_URL") ?? throw new InvalidOperationException("MISSING DISCORD_WEBHOOK_URL; check .env file.");

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();

    // Add Serilog for general logging with a higher minimum log level
    loggingBuilder.AddSerilog(new LoggerConfiguration()
        .MinimumLevel.Warning() // Set a higher minimum log level
        .MinimumLevel.Override("Microsoft", LogEventLevel.Error) // Override for specific categories
        .WriteTo.Console()
        .CreateLogger());
});

// builder.Services.AddDbContext<DataContext>(options =>
//     options.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=DiscordBotDB;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False"));

builder.Services.AddDbContext<DataContext>(options =>
   options.UseSqlServer("Data Source=mssql-db.noahnielsen.dk;Initial Catalog=DiscordBotDB;User ID=sa;Password=NoahsMSSQLDatabase123!;Connect Timeout=30;Encrypt=False;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False"));

DataContext dataContext;

/// <summary>
/// Start Database Connection using DataContext file
/// </summary>
try
{
    using (var scope = builder.Services.BuildServiceProvider().CreateScope())
    {
        dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        await dataContext.Database.MigrateAsync();
        await dataContext.SaveChangesAsync();
    }
    dbLogger.Information("Database migrated successfully");
}
catch (Exception ex)
{
    dbLogger.Error(ex, "Error migrating database");
}

builder.Services.AddSingleton<IConfiguration>(configuration);
// Configure Discord client
builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.All | GatewayIntents.MessageContent,
    LogLevel = LogSeverity.Debug
}));

// Configure InteractionService for handling interactions from commands, buttons, modals, and selects
builder.Services.AddSingleton(p => new InteractionService(p.GetRequiredService<DiscordSocketClient>()));

builder.Services.AddScoped<IRepository<Weapon>, Repository<Weapon>>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStrikeRepository, StrikeRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IBoughtWeaponRepository, BoughtWeaponRepository>();
builder.Services.AddScoped<IPaidAmountRepository, PaidAmountRepository>();

// Change Buttons service to Scoped
builder.Services.AddScoped<Buttons>();

// Add our command handler as a singleton service
builder.Services.AddSingleton<CommandHandler>();

builder.Services.AddHostedService<StrikeListService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<StrikeListService>>();
    var discordClient = provider.GetRequiredService<DiscordSocketClient>();
    ulong channelId = 1324100662073753726; // Replace with your channel ID
    ulong messageId = 0; // Replace with your message ID (initially set to 0 or a known message ID)
    return new StrikeListService(logger, provider, discordClient, channelId, messageId);
});

builder.Services.AddSingleton<BandeBuyService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<BandeBuyService>>();
    var serviceProvider = provider.GetRequiredService<IServiceProvider>();
    var discordClient = provider.GetRequiredService<DiscordSocketClient>();
    ulong channelId = 1333307808824688652; // Replace with your channel ID
    ulong messageId = 0; // Replace with your message ID (initially set to 0 or a known message ID)
    IEventRepository eventRepository = provider.GetRequiredService<IEventRepository>();
    IPaidAmountRepository paidAmountRepository = provider.GetRequiredService<IPaidAmountRepository>();
    return new BandeBuyService(logger, serviceProvider, discordClient, channelId, messageId, eventRepository, paidAmountRepository);
});

builder.Services.AddHostedService(provider => provider.GetRequiredService<BandeBuyService>());

// Configure CommandService for handling traditional text-based commands (e.g., !help)
// This is separate from InteractionService as it handles different types of commands
builder.Services.AddSingleton(new CommandService(new CommandServiceConfig
{
    // Make commands case-insensitive (e.g., !Help and !help work the same)
    CaseSensitiveCommands = false,
    // Set default run mode to async to prevent commands from blocking
    DefaultRunMode = Discord.Commands.RunMode.Async,
    // Log level matches our Discord client for consistency
    LogLevel = LogSeverity.Debug
}));
// Add our command handler as a singleton service
// This service will process messages and execute traditional text commands
builder.Services.AddSingleton<CommandHandler>();
// IHost represents the running application and its services that you just configured above.
IHost host = builder.Build();

// Loops through each service in IHostedService and starts them. This is a background service that executes logic for the IHost we just built.
foreach (IHostedService hostedService in host.Services.GetServices<IHostedService>())
{
    await hostedService.StartAsync(CancellationToken.None);
}

// Retrieve the DiscordSocketClient, InteractionService, and CommandHandler from the dependency injection container.
// These instances are used to interact with the Discord API and handle different types of commands.
DiscordSocketClient discordClient = host.Services.GetRequiredService<DiscordSocketClient>();
InteractionService interactionService = host.Services.GetRequiredService<InteractionService>();
CommandHandler commandHandler = host.Services.GetRequiredService<CommandHandler>();
CommandService commands = host.Services.GetRequiredService<CommandService>();

// Attach an event handler for when an interaction is created.This handler creates a context and executes the appropriate command.
// Learn more about the Interaction Framework: https://docs.discordnet.dev/guides/int_framework/intro.html
discordClient.InteractionCreated += async interaction =>
{
    SocketInteractionContext ctx = new(discordClient, interaction);
    await interactionService.ExecuteCommandAsync(ctx, host.Services);
};

// Hook MessageReceived so we can process each message to see if it qualifies as a text command
discordClient.MessageReceived += commandHandler.HandleCommandAsync;
// Hook CommandExecuted to handle post-execution logic for text commands
commands.CommandExecuted += commandHandler.OnCommandExecutedAsync;

// Attach the LogMessage to both discordClient.Log and interactionService.Log. This will direct logs to our logger and output them accordingly.
discordClient.Log += DiscordHelpers.LogMessageAsync;
interactionService.Log += DiscordHelpers.LogMessageAsync;

// When the bot is ready, this handler runs a helper method I made in DiscordHelpers.cs. It will take our Services we built earlier and finish building and logging in.
discordClient.Ready += async () =>
{
    // Initialize both interaction commands and traditional text commands
    await DiscordHelpers.ClientReady(host.Services);
};

// Initialize and register Classes that handle Discord events. I have all my event logic in one class (UserEvents) for organization.
UserEvents eventHandlers = new(discordClient);
eventHandlers.RegisterHandlers();

// Log in and authenticate with the Discord API using the bot token. Then it starts the client
await discordClient.LoginAsync(TokenType.Bot, discordToken);
await discordClient.StartAsync();

// This runs the IHost we configured earlier. It will run until the application is stopped.
await host.RunAsync();