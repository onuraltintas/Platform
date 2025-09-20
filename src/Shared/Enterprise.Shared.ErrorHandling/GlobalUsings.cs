// System
global using System.Diagnostics;
global using System.Globalization;
global using System.Net;
global using System.Text.Json;
global using System.Text.RegularExpressions;

// Microsoft Extensions
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.Extensions.Localization;
global using Microsoft.AspNetCore.Localization;

// ASP.NET Core
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Filters;
global using Microsoft.AspNetCore.Mvc.ModelBinding;

// Problem Details - removed invalid using

// Polly
global using Polly;
global using Polly.Extensions.Http;

// FluentValidation - aliased to avoid conflicts
global using FluentValidation;
global using FluentValidationException = FluentValidation.ValidationException;

// Database provider-agnostic base exception
global using System.Data.Common;

// Enterprise Shared
global using Enterprise.Shared.Logging.Interfaces;
global using Enterprise.Shared.Logging.Models;