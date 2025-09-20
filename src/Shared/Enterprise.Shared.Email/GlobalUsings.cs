// Global using directives for Enterprise.Shared.Email

global using System.ComponentModel.DataAnnotations;
global using System.Net;
global using System.Net.Mail;
global using System.Text.Json;
global using System.Text.RegularExpressions;
global using System.Collections.Concurrent;
global using System.Reflection;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.Diagnostics.HealthChecks;

global using Enterprise.Shared.Common.Models;
global using Enterprise.Shared.Common.Extensions;
global using Enterprise.Shared.Common.Exceptions;
global using Enterprise.Shared.Common.Enums;

global using Enterprise.Shared.Email.Interfaces;
global using Enterprise.Shared.Email.Models;
global using Enterprise.Shared.Email.Services;
global using Enterprise.Shared.Email.Configuration;

global using FluentEmail.Core;
global using FluentEmail.Core.Interfaces;
global using Scriban;
global using Scriban.Runtime;

global using ValidationResult = Enterprise.Shared.Email.Interfaces.ValidationResult;