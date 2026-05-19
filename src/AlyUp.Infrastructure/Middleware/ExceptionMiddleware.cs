using System.Net;
using System.Text.Json;
using FluentValidation;
using AlyUp.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace AlyUp.Infrastructure.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu uma exceção não tratada: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        if (exception is ValidationException validationException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            var errors = validationException.Errors
                .Select(error => new { error.PropertyName, error.ErrorMessage });
            var validationResponse = new { message = "Erro de validação.", errors };
            return context.Response.WriteAsync(JsonSerializer.Serialize(validationResponse));
        }

        // Exceções de Domínio
        if (exception is DomainException domainException)
        {
            context.Response.StatusCode = exception switch
            {
                EmailAlreadyExistsException => (int)HttpStatusCode.Conflict,
                SalonDocumentAlreadyExistsException => (int)HttpStatusCode.Conflict,
                _ => (int)HttpStatusCode.BadRequest
            };
            var response = new { message = domainException.Message };
            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        // Exceção de Credenciais Inválidas
        if (exception is InvalidCredentialsException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            var response = new { message = exception.Message };
            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        // Exceção de Usuário Inativo
        if (exception is UserInactiveException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            var response = new { message = exception.Message };
            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        // Exceção de Entidade Não Encontrada
        if (exception is EntityNotFoundException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            var response = new { message = exception.Message };
            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        // Exceções Genéricas
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        object finalResponse;
        if (_env.IsDevelopment())
        {
            finalResponse = new { message = exception.Message, details = exception.StackTrace?.ToString() };
        }
        else
        {
            finalResponse = new { message = "Ocorreu um erro interno no servidor." };
        }

        return context.Response.WriteAsync(JsonSerializer.Serialize(finalResponse));
    }
}
