using Microsoft.WindowsAzure.ResourceStack.Common.Storage;
using System.Collections;
using System.Collections.Generic;

namespace Xylab.Workflows.LogicApps.Engine
{
    public class SegmentedList<TModel> : IReadOnlyList<TModel>
    {
        private readonly TModel[] _model;

        public SegmentedList(SegmentedResult<TModel> segmentedResult)
        {
            _model = segmentedResult.Entities;
            ContinuationToken = segmentedResult.ContinuationToken;
        }

        public DataContinuationToken ContinuationToken { get; set; }

        public TModel this[int index] => _model[index];

        public int Count => _model.Length;

        public IEnumerator<TModel> GetEnumerator()
        {
            return ((IReadOnlyList<TModel>)_model).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IReadOnlyList<TModel>)_model).GetEnumerator();
        }
    }
}
