// System
global using System.Diagnostics;

// Xunit
global using Xunit;

// FluentAssertions
global using FluentAssertions;

// NSubstitute
global using NSubstitute;

// Microsoft Extensions
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Http.Features;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.ModelBinding;
global using Microsoft.AspNetCore.Localization;

// Enterprise ErrorHandling
global using Enterprise.Shared.ErrorHandling.Exceptions;
global using Enterprise.Shared.ErrorHandling.Models;
global using Enterprise.Shared.ErrorHandling.Handlers;
global using Enterprise.Shared.ErrorHandling.Middleware;
global using Enterprise.Shared.ErrorHandling.Extensions;

// Enterprise Shared
global using Enterprise.Shared.Logging.Interfaces;
global using Enterprise.Shared.Logging.Models;

// FluentValidation alias
global using FluentValidationException = FluentValidation.ValidationException;