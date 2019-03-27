#region Help:  Introduction to the Script Component
/* The Script Component allows you to perform virtually any operation that can be accomplished in
 * a .Net application within the context of an Integration Services data flow.
 *
 * Expand the other regions which have "Help" prefixes for examples of specific ways to use
 * Integration Services features within this script component. */
#endregion

#region Namespaces
using System;
using System.Data;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;

using Microsoft.SqlServer.Dts.Pipeline;
using System.Security.Cryptography;
using System.Text;

using System.IO;
#endregion

/// <summary>
/// This is the class to which to add your code.  Do not change the name, attributes, or parent
/// of this class.
/// </summary>
[Microsoft.SqlServer.Dts.Pipeline.SSISScriptComponentEntryPointAttribute]
public class ScriptMain : UserComponent
{
    private Encoding TextEncoding = System.Text.UTF8Encoding.UTF8;
    private const string ColumnDelimiter = ";";
    private const string TextQualifier = "\"";
    private const string TextQualifierEscape = "\"\"";

    private class ColumnClass
    {
        public int Index;
        public string Name;
    }

    private int ColumnCount, LastColumnIndex;
    private ColumnClass[] ColumnNameArray;

    private PipelineBuffer inputBuffer;
    string destination;

    private StreamWriter textWriter;


    #region Help:  Using Integration Services variables and parameters
    /* To use a variable in this script, first ensure that the variable has been added to
     * either the list contained in the ReadOnlyVariables property or the list contained in
     * the ReadWriteVariables property of this script component, according to whether or not your
     * code needs to write into the variable.  To do so, save this script, close this instance of
     * Visual Studio, and update the ReadOnlyVariables and ReadWriteVariables properties in the
     * Script Transformation Editor window.
     * To use a parameter in this script, follow the same steps. Parameters are always read-only.
     *
     * Example of reading from a variable or parameter:
     *  DateTime startTime = Variables.MyStartTime;
     *
     * Example of writing to a variable:
     *  Variables.myStringVariable = "new value";
     */
    #endregion

    #region Help:  Using Integration Services Connnection Managers
    /* Some types of connection managers can be used in this script component.  See the help topic
     * "Working with Connection Managers Programatically" for details.
     *
     * To use a connection manager in this script, first ensure that the connection manager has
     * been added to either the list of connection managers on the Connection Managers page of the
     * script component editor.  To add the connection manager, save this script, close this instance of
     * Visual Studio, and add the Connection Manager to the list.
     *
     * If the component needs to hold a connection open while processing rows, override the
     * AcquireConnections and ReleaseConnections methods.
     * 
     * Example of using an ADO.Net connection manager to acquire a SqlConnection:
     *  object rawConnection = Connections.SalesDB.AcquireConnection(transaction);
     *  SqlConnection salesDBConn = (SqlConnection)rawConnection;
     *
     * Example of using a File connection manager to acquire a file path:
     *  object rawConnection = Connections.Prices_zip.AcquireConnection(transaction);
     *  string filePath = (string)rawConnection;
     *
     * Example of releasing a connection manager:
     *  Connections.SalesDB.ReleaseConnection(rawConnection);
     */
    #endregion

    #region Help:  Firing Integration Services Events
    /* This script component can fire events.
     *
     * Example of firing an error event:
     *  ComponentMetaData.FireError(10, "Process Values", "Bad value", "", 0, out cancel);
     *
     * Example of firing an information event:
     *  ComponentMetaData.FireInformation(10, "Process Values", "Processing has started", "", 0, fireAgain);
     *
     * Example of firing a warning event:
     *  ComponentMetaData.FireWarning(10, "Process Values", "No rows were received", "", 0);
     */
    #endregion

    public override void AcquireConnections(object Transaction)
    {        
        IDTSConnectionManager100 connMgr = this.Connections.Connection;
        destination = (string)connMgr.AcquireConnection(null);        
    }

    private void WriteFieldContent(object fieldContent, Boolean appendColumnDelimiter)
    {
        string data = TextQualifier;
         
        if (fieldContent != null)
        {
            data += Convert.ToString(fieldContent).Replace(TextQualifier, TextQualifierEscape);
        }

        data += TextQualifier;

        if (appendColumnDelimiter)
        {
            data += ColumnDelimiter;
        }

        textWriter.Write(data);
    }

    private void WriteHeader()
    {
        if (this.Variables.varIsAppendMode)          
        {
            return;
        }

        for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
        {
            this.WriteFieldContent(ColumnNameArray[columnIndex].Name, (columnIndex != LastColumnIndex));
        }
        textWriter.WriteLine();
    }

    public override void ProcessInput(int InputID, Microsoft.SqlServer.Dts.Pipeline.PipelineBuffer Buffer)
    {
        // We need access to the PipelineBuffer which isn't exposed in ProcessInputRow
        inputBuffer = Buffer;

        base.ProcessInput(InputID, Buffer);
    }

    /// <summary>
    /// This method is called once, before rows begin to be processed in the data flow.
    ///
    /// You can remove this method if you don't need to do anything here.
    /// </summary>
    public override void PreExecute()
    {
        base.PreExecute();
        /*
         * Add your code here
         */
        textWriter = new StreamWriter(destination, this.Variables.varIsAppendMode, TextEncoding);

        ColumnCount = ComponentMetaData.InputCollection[0].InputColumnCollection.Count;
        LastColumnIndex = (ColumnCount - 1);
        ColumnNameArray = new ColumnClass[ColumnCount];
        int[] ColumnIndexes = GetColumnIndexes(ComponentMetaData.InputCollection[0].ID); // Same as InputID in ProcessInput
        int columnIndex = 0;
        foreach (IDTSInputColumn100 item in ComponentMetaData.InputCollection[0].InputColumnCollection)
        {
            ColumnNameArray[columnIndex] = new ColumnClass();
            ColumnNameArray[columnIndex].Name = Convert.ToString(item.Name);
            ColumnNameArray[columnIndex].Index = ColumnIndexes[ComponentMetaData.InputCollection[0].InputColumnCollection.GetObjectIndexByID(item.ID)];
            columnIndex++;
        }

        this.WriteHeader();
    }

    /// <summary>
    /// This method is called after all the rows have passed through this component.
    ///
    /// You can delete this method if you don't need to do anything here.
    /// </summary>
    public override void PostExecute()
    {
        base.PostExecute();
        /*
         * Add your code here
         */
        if (textWriter != null)
        {
            textWriter.Close();
            textWriter.Dispose();
        }
    }

    /// <summary>
    /// This method is called once for every row that passes through the component from Input0.
    ///
    /// Example of reading a value from a column in the the row:
    ///  string zipCode = Row.ZipCode
    ///
    /// Example of writing a value to a column in the row:
    ///  Row.ZipCode = zipCode
    /// </summary>
    /// <param name="Row">The row that is currently passing through the component</param>
    public override void Input0_ProcessInputRow(Input0Buffer Row)
    {
        /*
         * Add your code here
         */
                
        for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
        {            
            this.WriteFieldContent(inputBuffer[ColumnNameArray[columnIndex].Index], (columnIndex != LastColumnIndex));
        }

        textWriter.WriteLine();
    }

}
