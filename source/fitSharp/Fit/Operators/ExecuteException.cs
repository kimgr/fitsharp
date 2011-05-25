// FitNesse.NET
// Copyright � 2008 Syterra Software Inc. Includes work by Object Mentor, Inc., (c) 2002 Cunningham & Cunningham, Inc.
// This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License version 2.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

using System.Reflection;
using System.Text.RegularExpressions;
using fitSharp.Machine.Model;

namespace fitSharp.Fit.Operators {
    public class ExecuteException : InvokeCommandBase {
        private static readonly IdentifierName exceptionIdentifier = new IdentifierName("exception[");
        private static readonly Regex regexForMessageOnly = new Regex("^\".*\"$");
        private static readonly Regex regexForExceptionTypeNameOnly = new Regex("^.*: \".*\"$");

        public override bool CanExecute(ExecuteContext context, ExecuteParameters parameters) {
            return context.Command == ExecuteCommand.Check
                && exceptionIdentifier.IsStartOf(parameters.Cell.Text) && parameters.Cell.Text.EndsWith("]");
        }

        public override TypedValue Execute(ExecuteContext context, ExecuteParameters parameters) {
            string exceptionContent = parameters.Cell.Text.Substring("exception[".Length, parameters.Cell.Text.Length - ("exception[".Length + 1));
            try {
                GetActual(context, parameters);
                Processor.TestStatus.MarkWrong(parameters.Cell, "no exception");
            }
            catch (TargetInvocationException e) {
                if (IsMessageOnly(exceptionContent)) {
                    EvaluateException(e.InnerException.Message == exceptionContent.Substring(1, exceptionContent.Length - 2), parameters, e);
                }
                else if (IsExceptionTypeNameOnly(exceptionContent)) {
                    string actual = e.InnerException.GetType().Name + ": \"" + e.InnerException.Message + "\"";
                    EvaluateException(exceptionContent == actual, parameters, e);
                }
                else {
                    EvaluateException(e.InnerException.GetType().Name == exceptionContent, parameters, e);
                }
            }
            return TypedValue.Void;
        }

        private static bool IsExceptionTypeNameOnly(string exceptionContent) {
            return regexForExceptionTypeNameOnly.IsMatch(exceptionContent);
        }

        private static bool IsMessageOnly(string exceptionContent) {
            return regexForMessageOnly.IsMatch(exceptionContent);
        }

        private void EvaluateException(bool expression, ExecuteParameters parameters, TargetInvocationException e) {
            if (expression) {
                Processor.TestStatus.MarkRight(parameters.Cell);
            }
            else {
                Processor.TestStatus.MarkWrong(parameters.Cell, "exception[" + e.InnerException.GetType().Name + ": \"" + e.InnerException.Message + "\"]");
            }
        }
    }
}