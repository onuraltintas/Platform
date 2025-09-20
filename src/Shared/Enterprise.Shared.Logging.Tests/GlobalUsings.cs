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

// Serilog
global using Serilog.Core;
global using Serilog.Events;

// Enterprise Logging
global using Enterprise.Shared.Logging.Interfaces;
global using Enterprise.Shared.Logging.Models;
global using Enterprise.Shared.Logging.Services;
global using Enterprise.Shared.Logging.Extensions;
global using Enterprise.Shared.Logging.Extensions.Interceptors;
global using Enterprise.Shared.Logging.Enrichers;