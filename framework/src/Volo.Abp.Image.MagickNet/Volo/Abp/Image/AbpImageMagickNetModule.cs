﻿using Volo.Abp.Modularity;

namespace Volo.Abp.Image;

[DependsOn(typeof(AbpImageAbstractionsModule))]
public class AbpImageMagickNetModule : AbpModule
{
}