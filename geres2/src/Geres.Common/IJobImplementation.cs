//
// Copyright (c) Microsoft.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//           http://www.apache.org/licenses/LICENSE-2.0 
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;
using System.ComponentModel.Composition;
using Geres.Common.Entities;

namespace Geres.Common.Interfaces.Implementation
{
    [InheritedExport]
    public interface IJobImplementation
    {
        JobProcessResult DoWork(Job job, string jobPackagePath, string jobWorkingPath, Action<string> progressCallback);

        string JobType { get; }

        void CancelProcessCallback();
    }
}
