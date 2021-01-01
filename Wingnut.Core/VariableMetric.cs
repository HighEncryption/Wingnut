namespace Wingnut.Core
{
    internal enum VariableDataType
    {
        Integer = 0,
        Double = 1
    }

    internal class VariableMetric
    {
        public string VariableName { get; set; }

        public VariableDataType DataType { get; set; }

        public string TableName { get; set; }

        public bool Initialized { get; set; }

        public VariableMetric(
            string variableName,
            VariableDataType dataType = VariableDataType.Double,
            string tableName = null)
        {
            this.VariableName = variableName;
            this.DataType = dataType;

            if (tableName == null)
            {
                tableName = variableName.Replace('.', '_');
            }

            this.TableName = tableName;
        }
    }
}