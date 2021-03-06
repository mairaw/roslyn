﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    [Export(typeof(IWorkspaceProjectContextFactory))]
    internal partial class CPSProjectFactory : IWorkspaceProjectContextFactory
    {
        private readonly VisualStudioProjectFactory _projectFactory;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IProjectCodeModelFactory _projectCodeModelFactory;
        private readonly ExternalErrorDiagnosticUpdateSource _externalErrorDiagnosticUpdateSource;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CPSProjectFactory(
            VisualStudioProjectFactory projectFactory,
            VisualStudioWorkspaceImpl workspace,
            IProjectCodeModelFactory projectCodeModelFactory,
            [Import(AllowDefault = true)] /* not present in unit tests */ ExternalErrorDiagnosticUpdateSource externalErrorDiagnosticUpdateSource)
        {
            _projectFactory = projectFactory;
            _workspace = workspace;
            _projectCodeModelFactory = projectCodeModelFactory;
            _externalErrorDiagnosticUpdateSource = externalErrorDiagnosticUpdateSource;
        }

        IWorkspaceProjectContext IWorkspaceProjectContextFactory.CreateProjectContext(
            string languageName,
            string projectUniqueName,
            string projectFilePath,
            Guid projectGuid,
            object hierarchy,
            string binOutputPath)
        {
            var visualStudioProject = CreateVisualStudioProject(languageName, projectUniqueName, projectFilePath, projectGuid);
            return new CPSProject(visualStudioProject, _workspace, _projectCodeModelFactory, _externalErrorDiagnosticUpdateSource, projectGuid, binOutputPath);
        }

        private VisualStudioProject CreateVisualStudioProject(string languageName, string projectUniqueName, string projectFilePath, Guid projectGuid)
        {
            var creationInfo = new VisualStudioProjectCreationInfo
            {
                FilePath = projectFilePath,
                ProjectGuid = projectGuid,
            };

            var visualStudioProject = _projectFactory.CreateAndAddToWorkspace(projectUniqueName, languageName, creationInfo);

            if (languageName == LanguageNames.FSharp)
            {
                var shell = (IVsShell)ServiceProvider.GlobalProvider.GetService(typeof(SVsShell));

                // Force the F# package to load; this is necessary because the F# package listens to WorkspaceChanged to 
                // set up some items, and the F# project system doesn't guarantee that the F# package has been loaded itself
                // so we're caught in the middle doing this.
                shell.LoadPackage(Guids.FSharpPackageId, out var unused);
            }

            return visualStudioProject;
        }
    }
}
