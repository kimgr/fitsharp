using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fitSharp.Machine.Model;
using fitSharp.Fit.Model;
using System.Diagnostics;

namespace fitSharp.Machine.Engine {
    public class InvokeProfile<T,P> : Operator<T,P>, InvokeOperator<T> where P : class, Processor<T> {
        private InvokeOperator<T> target = null;

        public bool CanInvoke(TypedValue instance, string memberName, Tree<T> parameters) {
            return Target.CanInvoke(instance, memberName, parameters);
        }

        public TypedValue Invoke(TypedValue instance, string memberName, Tree<T> parameters) {
            using (new ScopedInvocationLogger(Processor.Memory.GetItem<FixtureProfiler>().ConsoleOutputPrefix, 
                                            GetFullName(instance, memberName, parameters))) {
                return Target.Invoke(instance, memberName, parameters);
            }
        }

        private string GetFullName(TypedValue instance, string memberName, Tree<T> parameters) {
            RuntimeMember member;

            if (memberName.StartsWith(MemberName.NamedParameterPrefix)) {
                var parameterNames = parameters.Branches.Alternate().Aggregate(new List<string>(),
                                                          (names, parameter) =>
                                                          {
                                                              names.Add(Processor.ParseTree<T, string>(parameter));
                                                              return names;
                                                          });
                member = RuntimeType.FindInstance(instance.Value, new IdentifierName(memberName.Substring(MemberName.NamedParameterPrefix.Length)), parameterNames);
            }
            else {
                member = RuntimeType.FindInstance(instance.Value, new IdentifierName(memberName), parameters.Branches.Count);
            }

            if (member == null)
                return null;

            return instance.Value.GetType().FullName + "." + member.Name;
        }

        private InvokeOperator<T> Target {
            get {
                if (target == null)
                    target = new InvokeDefault<T, P>() { Processor = Processor };
                
                return target;
            }
        }
    }

    public class ScopedInvocationLogger : IDisposable {
        private Stopwatch stopwatch;
        private string logPrefix;
        private string fixtureMember;

        public ScopedInvocationLogger(string logPrefix, string fixtureMember) {
            this.logPrefix = logPrefix ?? string.Empty;
            this.fixtureMember = fixtureMember;
            MaybeWriteInvoking();

            this.stopwatch = new Stopwatch();
            this.stopwatch.Start();
        }

        public void Dispose() {
            MaybeWriteInvoked();
            GC.SuppressFinalize(this);
        }

        private void MaybeWriteInvoking() {
            if (fixtureMember != null)
                Console.WriteLine("{0}: {1}", Prelude("Invoking"), fixtureMember);
        }

        private void MaybeWriteInvoked() {
            if (fixtureMember != null)
                Console.WriteLine("{0}: {1}, took {2} ms", Prelude("Invoked"), fixtureMember, stopwatch.ElapsedMilliseconds);
        }

        private string Prelude(string title) {
            return string.Format("{0}: {1}{2}", DateTime.Now, logPrefix, title);
        }
    }
}
