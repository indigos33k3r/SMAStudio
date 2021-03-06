﻿using SMAStudiovNext.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Models
{
    public class ModuleModelProxy : ModelProxyBase
    {
        public ModuleModelProxy(object obj, IBackendContext backendContext)
        {
            instance = obj;
            instanceType = instance.GetType();

            Context = backendContext;
        }

        public Guid ModuleID
        {
            get
            {
                var property = GetProperty("ModuleID");
                return (Guid)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("ModuleID");
                property.SetValue(instance, value);
            }
        }

        public string ModuleName
        {
            get
            {
                var property = GetProperty("ModuleName");
                return (string)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("ModuleName");
                property.SetValue(instance, value);
            }
        }

        public string ModuleUrl
        {
            get
            {
                var property = GetProperty("ModuleUrl");
                return (string)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("ModuleUrl");
                property.SetValue(instance, value);
            }
        }

        public string ModuleVersion
        {
            get
            {
                var property = GetProperty("ModuleVersion");
                return (string)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("ModuleVersion");
                property.SetValue(instance, value);
            }
        }
    }
}
