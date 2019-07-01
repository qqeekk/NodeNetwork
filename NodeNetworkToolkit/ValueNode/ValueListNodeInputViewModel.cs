using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Alias;
using DynamicData.Kernel;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using ReactiveUI;

namespace NodeNetwork.Toolkit.ValueNode
{
    /// <summary>
    /// A node input that keeps a list of the latest values produced by all of the connected ValueNodeOutputViewModels.
    /// This input can take multiple connections, ValueNodeInputViewModel cannot.
    /// </summary>
    /// <typeparam name="T">The type of object this input can receive</typeparam>
    public class ValueListNodeInputViewModel<T> : NodeInputViewModel
    {
        static ValueListNodeInputViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeInputView(), typeof(IViewFor<ValueListNodeInputViewModel<T>>));
        }

        /// <summary>
        /// The current values of the outputs connected to this input
        /// </summary>
        public IObservableList<T> Values { get; }

        public ValueListNodeInputViewModel()
        {
            MaxConnections = Int32.MaxValue;
            ConnectionValidator = pending => new ConnectionValidationResult(
                pending.Output is ValueNodeOutputViewModel<T> ||
                pending.Output is ValueNodeOutputViewModel<IObservableList<T>>,
                null
            );

            var valuesFromSingles = Connections.Connect(c => c.Output is ValueNodeOutputViewModel<T>)
                .Transform(c => ((ValueNodeOutputViewModel<T>) c.Output))
                //Note: this line used to be
                //.AutoRefresh(output => output.CurrentValue)
                //which ignored changes where CurrentValue didn't change.
                //This caused problems when the value object isn't replaced, but one of its properties changes.
                .AutoRefreshOnObservable(output => output.Value)
                .Transform(output => output.CurrentValue, true)
                /*.Select((IChangeSet<T> changes) =>
                {
                    if (changes.TotalChanges == changes.Replaced + changes.Refreshes)
                    {
                        bool allRefresh = true;
                        var newChanges = new ChangeSet<T>();
                        foreach (var change in changes)
                        {
                            if (change.Reason == ListChangeReason.Replace)
                            {
                                if (change.Type == ChangeType.Item)
                                {
                                    if (change.Item.Previous != change.Item.Current)
                                    {
                                        allRefresh = false;
                                        break;
                                    }
                                    newChanges.Add(new Change<T>(ListChangeReason.Refresh, change.Item.Current, change.Item.Previous, change.Item.CurrentIndex, change.Item.PreviousIndex));
                                }
                                else
                                {
                                    throw new Exception("Does this ever occur?");
                                    //for(int i = change.Range.Index; i < )
                                }
                            }
                            else
                            {
                                newChanges.Add(change);
                            }
                        }

                        if (allRefresh) return newChanges;
                    }
                    return changes;
                })*/;
                

            Connections.Connect(c => c.Output is ValueNodeOutputViewModel<IObservableList<T>>)
                .Transform(c => ((ValueNodeOutputViewModel<IObservableList<T>>) c.Output).Value.Switch())
                .Bind(out var aggregate)
                .Subscribe();
            var valuesFromAggregate = aggregate.Or();

            //TODO: fix problem:
            //Or deduplicates events by checking if an element is already in the list and silently ignoring the event in that case.
            //this unfortunately also breaks propagation of changes through the system.
            //Find a way to reinject the events, modify the Or, change the propagation mechanics.
            //Maybe the root cause of this issue are replacement changes that should be refreshes?
            
            Values = valuesFromSingles.Or(valuesFromAggregate).AsObservableList();
        }
    }
}
