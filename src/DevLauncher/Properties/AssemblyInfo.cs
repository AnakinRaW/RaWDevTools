﻿using System.Runtime.CompilerServices;
using AnakinRaW.AppUpdaterFramework.Attributes;

[assembly:UpdateProduct("Republic at War DevLauncher")]
[assembly:UpdateComponent("RawDevLauncher.Exe", Name = "Republic at War DevLauncher")]

[assembly:InternalsVisibleTo("DevLauncher.Tests")]