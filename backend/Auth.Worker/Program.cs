using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Auth.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var awsRegion = context.Configuration["Aws:Region"];

        if (string.IsNullOrWhiteSpace(awsRegion))
        {
            throw new InvalidOperationException("Configuração Aws:Region não encontrada no appsettings.json");
        }

        services.AddSingleton<IAmazonSQS>(_ =>
            new AmazonSQSClient(RegionEndpoint.GetBySystemName(awsRegion)));

        services.AddHostedService<UserRegisteredConsumer>();
    })
    .Build();

await host.RunAsync();

public class UserRegisteredConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly ILogger<UserRegisteredConsumer> _logger;
    private readonly string _queueUrl;

    public UserRegisteredConsumer(
        IAmazonSQS sqs,
        ILogger<UserRegisteredConsumer> logger,
        IConfiguration config)
    {
        _sqs = sqs;
        _logger = logger;

        _queueUrl = config["Aws:UserRegisteredQueueUrl"];

        if (string.IsNullOrWhiteSpace(_queueUrl))
        {
            _logger.LogError("Configuração Aws:UserRegisteredQueueUrl NÃO encontrada no appsettings.json");
            throw new InvalidOperationException("Aws:UserRegisteredQueueUrl não configurada.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UserRegisteredConsumer started. QueueUrl={QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 5,
                    WaitTimeSeconds = 10
                };

                var response = await _sqs.ReceiveMessageAsync(receiveRequest, stoppingToken);

                if (response == null)
                {
                    _logger.LogWarning("ReceiveMessageAsync retornou null");
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                if (response.Messages == null || response.Messages.Count == 0)
                {
                    // Sem mensagens, espera um pouco e tenta de novo
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                foreach (var msg in response.Messages)
                {
                    if (msg == null)
                    {
                        _logger.LogWarning("Mensagem nula recebida.");
                        continue;
                    }

                    try
                    {
                        _logger.LogInformation("Mensagem recebida: {Body}", msg.Body);

                        var body = JsonSerializer.Deserialize<UserRegisteredMessage>(msg.Body);

                        if (body == null)
                        {
                            _logger.LogWarning(
                                "Não foi possível desserializar o body para UserRegisteredMessage. Body={Body}",
                                msg.Body);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "Processando usuário registrado: {Email}, {RegisteredAt}",
                                body.Email,
                                body.RegisteredAt);
                        }

                        // Depois de processar, apaga da fila
                        await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "Erro de JSON ao processar mensagem: {Body}", msg.Body);

                        // Apaga mensagem ruim para não travar o worker
                        await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar mensagem: {Body}", msg.Body);

                        // Também apaga em qualquer outro erro
                        await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                    }
                }
            }
            catch (AmazonSQSException ex)
            {
                _logger.LogError(ex,
                    "Erro SQS. Code={Code}, StatusCode={StatusCode}, Message={Message}",
                    ex.ErrorCode, ex.StatusCode, ex.Message);

                await Task.Delay(5000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no consumer");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
