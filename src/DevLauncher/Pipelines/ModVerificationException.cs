﻿using System;
using System.Collections.Generic;
using System.Text;
using AnakinRaW.CommonUtilities.SimplePipeline;
using RepublicAtWar.DevLauncher.Pipelines.Steps.Verification;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal sealed class ModVerificationException(IEnumerable<ModVerificationStep> failedSteps) : Exception
{
    private readonly string? _error = null;
    private readonly IEnumerable<IStep> _failedSteps = failedSteps ?? throw new ArgumentNullException(nameof(failedSteps));

    /// <inheritdoc/>
    public override string Message => Error;

    private string Error
    {
        get
        {
            if (_error != null)
                return _error;
            var stringBuilder = new StringBuilder();

            foreach (var step in _failedSteps)
                stringBuilder.Append($"Verification step '{step}' has errors;");
            return stringBuilder.ToString().TrimEnd(';');
        }
    }
}