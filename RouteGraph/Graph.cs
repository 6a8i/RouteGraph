using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace RouteGraph
{
    public interface IGraph<T>
    {
        IObservable<IEnumerable<T>> RoutesBetween(T source, T target);
    }

    public class Graph<T> : IGraph<T>
    {
        public IEnumerable<ILink<T>> Links { get; private set; }

        // Aux attribute
        private readonly LinkedList<T> visitedNodes;

        // Aux attribute
        private readonly List<List<T>> paths;

        public Graph(IEnumerable<ILink<T>> links)
        {
            Links = links;
            visitedNodes = new LinkedList<T>();
            paths = new List<List<T>>();
        }

        public IObservable<IEnumerable<T>> RoutesBetween(T source, T target)
        {
            // I don't know how this class really works, this was the first time I used. I read a little about, just to understand
            // and well, I learned that it was design to use with the observer design pattern that is a generalized mechanism for
            // push-based notification.
            // Here is created the observable class of IEnumerable<T> and start a new task that will calculate the paths from source
            // node until the target node.
            return Observable.Create<IEnumerable<T>>(
                (observer, cancellationToken) => Task.Factory.StartNew(() =>
                {
                    // In a aux attribute is saved the main source node.
                    visitedNodes.AddFirst(source);

                    // Then is called the recursive method that will explore the possibles paths and save them.
                    getRoutesUntilTarget(source, target);

                    // As it is done, we read each possible path and add it to the observable. I think the OnNext method is like
                    // "notifing" the observable object. 
                    paths.ForEach((path) => observer.OnNext(path));

                    // Finaly, set the observable to clomplete, so the task is done and the return goes on.
                    observer.OnCompleted();
                },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default)
            );

            // I think there is no need to make this sub method a main method, for now, only this method (RoutesBetween) will
            // use this, so I think a sub method makes sense.
            void getRoutesUntilTarget(T internalSource, T internalTarget)
            {
                // Gets all the possible links of the last node added to the aux attribute.
                var nodes = Links.Where(x => x.Source.Equals(visitedNodes.LastOrDefault()));

                // Read node per node
                foreach (var node in nodes)
                {
                    // Verify if the target of the node is already in the aux attribute.
                    if (visitedNodes.Contains(node.Target))
                    {
                        // if so, then continue to the next node what doesn't exist, forcing to end the foreach clause.
                        continue;
                    }
                    // Verify if the actual node target is the same as the final target
                    if (node.Target.Equals(target))
                    {
                        // If so, add it to the list of paths and continue to the next.
                        visitedNodes.AddLast(node.Target);
                        savePath();
                        visitedNodes.RemoveLast();
                        continue;
                    }
                    // If none of them above was true, then we are in the middle of the path, save as visited and lets search
                    // the next node after the actual, until gets the target.
                    visitedNodes.AddLast(node.Target);
                    getRoutesUntilTarget(node.Target, target);
                    visitedNodes.RemoveLast();
                }
            }

            // I think there is no need to make this sub method a main method, for now, only this method (RoutesBetween) will
            // use this, so I think a sub method makes sense.
            void savePath()
            {
                // Just save the path in a list of paths.
                paths.Add(visitedNodes.ToList());
            }
        }
    }
}
