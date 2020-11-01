using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace DUBuild.DU
{
    public class DependencyTree
    {
        public class MissingDependencyException : Exception
        {
            public MissingDependencyException(string message) : base(message) { }
        }

        private Dictionary<SourceFile, uint> dependencyTree;
        private SourceRepository sourceRepository;

        private int antiLoopCounter;

        public DependencyTree(SourceRepository sources)
        {
            dependencyTree = new Dictionary<SourceFile, uint>();
            sourceRepository = sources;
            antiLoopCounter = 0;
        }

        public void Add(string dependencyClassName, SourceFile parent = null)
        {
            if (!sourceRepository.ClassExists(dependencyClassName)) throw new MissingDependencyException(String.Format("Dependency {0} required by {1} not found in source tree", dependencyClassName, parent?.ClassName??"Unknown"));
            var dependency = sourceRepository.GetByClassname(dependencyClassName);
            Add(dependency, parent);
        }
        public void Add(SourceFile dependency, SourceFile parent = null)
        {

            if (!dependencyTree.ContainsKey(dependency))
            {
                var startingDependencyValue = dependencyTree.ContainsKey(parent??new SourceFile()) ? dependencyTree[parent] : 0;
                dependencyTree.Add(dependency, startingDependencyValue);
            }

            dependencyTree[dependency] += 1;
            foreach (var recursiveDependency in dependency.Dependencies)
            {
                antiLoopCounter++;
                if (antiLoopCounter > 100)
                {
                    throw new Exception("Possible dependency loop detected");
                }
                Add(recursiveDependency, dependency);
            }

            antiLoopCounter = 0;
        }

        public IOrderedEnumerable<KeyValuePair<SourceFile, uint>> GetDependencyOrder()
        {
            return dependencyTree.OrderByDescending(x => x.Value);
        }
    }
}
