// System
global using System.ComponentModel.DataAnnotations;
global using System.Diagnostics;
global using System.Reflection;
global using System.Security.Claims;
global using System.Text.Json;

// Microsoft Extensions
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;

// ASP.NET Core
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Http;

// Serilog
global using Serilog;
global using Serilog.Core;
global using Serilog.Events;

// Castle
global using Castle.DynamicProxy;

// Enterprise Common
global using Enterprise.Shared.Common.Models;