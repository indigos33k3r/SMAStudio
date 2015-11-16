﻿using Caliburn.Micro;
using Gemini.Framework.Services;
using Gemini.Modules.ErrorList;
using Gemini.Modules.Output;
using SMAStudiovNext.Core;
using SMAStudiovNext.Icons;
using SMAStudiovNext.Modules.Runbook.CodeCompletion;
using SMAStudiovNext.Modules.Runbook.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMAStudiovNext.Agents
{
    /// <summary>
    /// Checks the currently active runbook for parse errors and populates the error list
    /// in case of errors AND parses the runbook in order to provide auto completion for our auto completion
    /// agent.
    /// </summary>
    public class RunbooksScanningAgent : IAgent
    {
        private readonly IErrorList _errorList;
        private readonly IOutput _output;
        private readonly IShell _shell;

        private readonly object _syncLock = new object();
        private readonly Thread _backgroundThread;
        private bool _isRunning = true;

        private readonly IDictionary<string, int> _errorStates;

        public RunbooksScanningAgent()
        {
            _errorStates = new Dictionary<string, int>();

            _backgroundThread = new Thread(new ThreadStart(StartInternal));
            _backgroundThread.Priority = ThreadPriority.BelowNormal;

            _errorList = IoC.Get<IErrorList>();
            _output = IoC.Get<IOutput>();
            _shell = IoC.Get<IShell>();
        }

        /// <summary>
        /// Entry point for agent
        /// </summary>
        public void Start()
        {
            _backgroundThread.Start();
        }

        /// <summary>
        /// Runs our background thread where the parsing of the runbook is done. If there is parse errors in our runbook,
        /// tokens will be null and we won't be able to populate our auto complete engine with information about variables etc.
        /// </summary>
        private void StartInternal()
        {
            Token[] tokens;
            ParseError[] parseErrors;

            while (_isRunning)
            {
                if (_shell.ActiveItem != null && (_shell.ActiveItem is RunbookViewModel))
                {
                    var content = ((RunbookViewModel)_shell.ActiveItem).Content;

                    // Run the script through the PS parsers
                    var scriptBlock = System.Management.Automation.Language.Parser.ParseInput(content, out tokens, out parseErrors);

                    if (_errorList != null)
                        ParseRunbookForErrors(parseErrors);

                    if (tokens != null && tokens.Length > 0)
                    {
                        if (_shell.ActiveItem is RunbookViewModel)
                            ParseRunbook(((RunbookViewModel)_shell.ActiveItem), tokens);
                    }
                }

                Thread.Sleep(2 * 1000);
            }
        }

        private void ParseRunbook(RunbookViewModel runbookViewModel, Token[] tokens)
        {
            // Clean out
            runbookViewModel.CodeCompletionContext.Variables.Clear();

            int idx = 0;
            foreach (var token in tokens)
            {
                switch (token.Kind)
                {
                    case TokenKind.Variable:
                        if (token.Extent.StartOffset > runbookViewModel.CaretOffset)
                            continue;

                        if (idx > 2 && idx < (tokens.Length - 2))
                        {
                            // We might have a definition of what this variable contains
                            if (tokens[idx - 1].Text.Equals("]"))
                            {
                                var identifier = tokens[idx - 2];

                                var variableObject = new VariableCompletionData(token.Text, identifier.Text);

                                if (runbookViewModel.CodeCompletionContext.Variables.FirstOrDefault(v => v.Equals(variableObject)) == null)
                                    runbookViewModel.CodeCompletionContext.Variables.Add(variableObject);
                            }
                            else
                            {
                                var variableObject = new VariableCompletionData(token.Text, ""); 

                                if (runbookViewModel.CodeCompletionContext.Variables.FirstOrDefault(v => v.Equals(variableObject)) == null)
                                    runbookViewModel.CodeCompletionContext.Variables.Add(variableObject);
                            }
                        }
                        else
                        {
                            var variableObject = new VariableCompletionData(token.Text, "");

                            if (runbookViewModel.CodeCompletionContext.Variables.FirstOrDefault(v => v.Equals(variableObject)) == null)
                                runbookViewModel.CodeCompletionContext.Variables.Add(variableObject);
                        }

                        break;
                }

                switch (token.TokenFlags)
                {
                    case TokenFlags.CommandName:
                        // TODO: Try to retrieve the parameters for the cmdlet/runbook
                        if (token.Text.Equals("import-module", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (tokens.Length > idx)
                            {
                                var moduleToImport = tokens[idx + 1];

                                if (moduleToImport.Text.Length > 0)
                                    runbookViewModel.CodeCompletionContext.AddModule(runbookViewModel.DisplayName.Replace("*", ""), moduleToImport.Text);
                            }
                        }
                        break;
                }

                idx++;
            }
        }

        /// <summary>
        /// Handles notifying the user that there is somekind of parse error in the runbook.
        /// </summary>
        private void ParseRunbookForErrors(ParseError[] parseErrors)
        {
            bool hasNewErrors = true;
            var hashCode = 0;
            foreach (var error in parseErrors)
                hashCode += error.Message.Length + error.Extent.StartLineNumber + error.Extent.StartColumnNumber;

            if (_errorStates.ContainsKey(_shell.ActiveItem.ContentId))
            {
                if (hashCode.Equals(_errorStates[_shell.ActiveItem.ContentId]))
                    hasNewErrors = false;
            }

            if (parseErrors.Length > 0 && hasNewErrors)
            {
                NotifyParseErrors((RunbookViewModel)_shell.ActiveItem, parseErrors, hashCode);
            }
            else if (parseErrors.Length == 0 && _errorStates.ContainsKey(_shell.ActiveItem.ContentId) && _errorStates[_shell.ActiveItem.ContentId] > 0)
            {
                ClearParseErrors((RunbookViewModel)_shell.ActiveItem);
            }
        }

        /// <summary>
        /// Populates the error list with errors
        /// </summary>
        /// <param name="runbookViewModel"></param>
        /// <param name="parseErrors"></param>
        /// <param name="hashCode"></param>
        private void NotifyParseErrors(RunbookViewModel runbookViewModel, ParseError[] parseErrors, int hashCode)
        {
            ClearParseErrors(runbookViewModel);

            foreach (var error in parseErrors)
            {
                _errorList.AddItem(
                    ErrorListItemType.Error,
                    error.Message,
                    runbookViewModel.DisplayName,
                    error.Extent.StartLineNumber,
                    error.Extent.StartColumnNumber
                );
            }

            if (_errorStates.ContainsKey(runbookViewModel.ContentId))
                _errorStates[runbookViewModel.ContentId] = hashCode;
            else
                _errorStates.Add(runbookViewModel.ContentId, hashCode);
        }

        /// <summary>
        /// Clears errors from the error list
        /// </summary>
        /// <param name="runbookViewModel"></param>
        private void ClearParseErrors(RunbookViewModel runbookViewModel)
        {
            var errors = _errorList.Items;
            var toRemove = new List<ErrorListItem>();

            foreach (var error in errors)
            {
                if (error.Path.Equals(runbookViewModel.DisplayName))
                    toRemove.Add(error);
            }

            _errorList.Items.RemoveRange(toRemove);

            if (_errorStates.ContainsKey(runbookViewModel.ContentId))
            {
                _errorStates[runbookViewModel.ContentId] = 0;
            }
        }

        /// <summary>
        /// Called when the agent should stop
        /// </summary>
        public void Stop()
        {
            lock (_syncLock)
            {
                _isRunning = true;
                _backgroundThread.Abort();
            }
        }
    }
}