using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.IO;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;

using Rhino.DocObjects;
using Rhino.Collections;
using GH_IO;
using GH_IO.Serialization;

/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance
{
#region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { __out.Add(text); }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { __out.Add(string.Format(format, args)); }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { __out.Add(GH_ScriptComponentUtilities.ReflectType_CS(obj)); }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { __out.Add(GH_ScriptComponentUtilities.ReflectType_CS(obj, method_name)); }
#endregion

#region Members
  /// <summary>Gets the current Rhino document.</summary>
  private RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private IGH_Component Component; 
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private int Iteration;
#endregion

  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments, 
  /// Output parameters as ref arguments. You don't have to assign output parameters, 
  /// they will have a default value.
  /// </summary>
  private void RunScript(int rx, int ry, Point3d p1, Point3d p2, int mod, int spid, ref object _pts, ref object _polyline, ref object _text, ref object _length, ref object _timecost)
  {
    
    Grid2pts(p1, p2, rx, ry);

    int k = 0;
    int i, j;
    double d1 = (ry - 1) / 6;
    double d2 = (rx - 1) / 4;
    double ny = Math.Floor(d1);
    double nx = Math.Floor(d2);


    StartMill();

    for(j = 0; j < ry ; j++){
      for(i = 0; i < rx  ; i++){
        k = j * rx + i;
        if( j < ny * 6 + 1 && i < 4 * nx){

          if( j % 12 < 7){
            if(i % 4 == 2 && j % 6 == 2){

              // draw a parallelogram

              addPath(k);
              addPath(k + 4 * rx);
              addPath(k + 3 * rx + 2);
              addPath(k - rx + 2);
              addPath(k);

              // draw a second parallelogram

              addPath(k - 2 * rx);
              addPath(k - rx - 2);
              addPath(k + 3 * rx - 2);
              addPath(k + 2 * rx);

              // draw triangle
              addPath(k + rx - 2);
              addPath(k);
              addPath(k + rx + 2);
              addPath(k + 2 * rx);
            }}

          if(j % 12 > 6){
            if(i % 4 == 2 && j % 6 == 2){
              
              int interval = (int)Math.Round(rx - 4 * nx-1);
              int m = k + rx - 1 - 2 * i- interval;

              // change starting point , draw a parallelogram

              addPath(m);
              addPath(m + 4 * rx);
              addPath(m + 3 * rx + 2);
              addPath(m - rx + 2);
              addPath(m);

              // draw a second parallelogram

              addPath(m - 2 * rx);
              addPath(m - rx - 2);
              addPath(m + 3 * rx - 2);
              addPath(m + 2 * rx);

              // draw triangle
              addPath(m + rx - 2);
              addPath(m);
              addPath(m + rx + 2);
              addPath(m + 2 * rx);
            }}
        }
      }
    }


    endMill();
    CalcTime();

    _pts = pts;
    _polyline = poly;
    _text = text;
    _length = poly.Length;
    _timecost = time;

  }

  // <Custom additional code> 
  
  List<Point3d> pts = new List<Point3d>();
  Polyline poly = new Polyline();
  List<string> text = new List<string>();
  double time = 0.0;

  void StartMill(){
    text.Add("start milling");
    return;
  }

  void addPath(int index){
    if(index < 0 || index >= (pts.Count)) return;
    poly.Add(pts[index]);
    text.Add("X " + pts[index].X + " Y " + pts[index].Y + " Z " + pts[index].Z);
    return;
  }

  void addPath2(int index, double scale){
    if(index < 0 || index >= (pts.Count)) return;
    Point3d p = new Point3d(pts[index].X, pts[index].Y, poly.Count * scale);
    poly.Add(p);
    text.Add("X " + p.X + " Y " + p.Y + " Z " + p.Z);
    return;
  }

  void endMill(){
    text.Add("end milling");
    return;
  }

  void CalcTime(){
    poly.MergeColinearSegments(0.01, false);
    time = poly.Length + poly.Count * 3;
  }

  // this is a function that draws grid of points from two min, max points
  void Grid2pts(Point3d p1, Point3d p2, int rx, int ry){
    pts.Clear();
    poly.Clear();

    text.Clear();
    time = 0.0;

    double dy = (p2.Y - p1.Y) / (ry - 1.0);
    double dx = (p2.X - p1.X) / (rx - 1.0);
    int i, j;
    for(j = 0; j < ry; j++){
      for(i = 0; i < rx; i++){
        Point3d pt = new Point3d(p1.X + i * dx, p1.Y + j * dy, p1.Z);
        pts.Add(pt);
      }
    }
    return;
  }

  // </Custom additional code> 

  private List<string> __err = new List<string>(); //Do not modify this list directly.
  private List<string> __out = new List<string>(); //Do not modify this list directly.
  private RhinoDoc doc = RhinoDoc.ActiveDoc;       //Legacy field.
  private IGH_ActiveObject owner;                  //Legacy field.
  private int runCount;                            //Legacy field.
  
  public override void InvokeRunScript(IGH_Component owner, object rhinoDocument, int iteration, List<object> inputs, IGH_DataAccess DA)
  {
    //Prepare for a new run...
    //1. Reset lists
    this.__out.Clear();
    this.__err.Clear();

    this.Component = owner;
    this.Iteration = iteration;
    this.GrasshopperDocument = owner.OnPingDocument();
    this.RhinoDocument = rhinoDocument as Rhino.RhinoDoc;

    this.owner = this.Component;
    this.runCount = this.Iteration;
    this. doc = this.RhinoDocument;

    //2. Assign input parameters
        int rx = default(int);
    if (inputs[0] != null)
    {
      rx = (int)(inputs[0]);
    }

    int ry = default(int);
    if (inputs[1] != null)
    {
      ry = (int)(inputs[1]);
    }

    Point3d p1 = default(Point3d);
    if (inputs[2] != null)
    {
      p1 = (Point3d)(inputs[2]);
    }

    Point3d p2 = default(Point3d);
    if (inputs[3] != null)
    {
      p2 = (Point3d)(inputs[3]);
    }

    int mod = default(int);
    if (inputs[4] != null)
    {
      mod = (int)(inputs[4]);
    }

    int spid = default(int);
    if (inputs[5] != null)
    {
      spid = (int)(inputs[5]);
    }



    //3. Declare output parameters
      object _pts = null;
  object _polyline = null;
  object _text = null;
  object _length = null;
  object _timecost = null;


    //4. Invoke RunScript
    RunScript(rx, ry, p1, p2, mod, spid, ref _pts, ref _polyline, ref _text, ref _length, ref _timecost);
      
    try
    {
      //5. Assign output parameters to component...
            if (_pts != null)
      {
        if (GH_Format.TreatAsCollection(_pts))
        {
          IEnumerable __enum__pts = (IEnumerable)(_pts);
          DA.SetDataList(1, __enum__pts);
        }
        else
        {
          if (_pts is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(_pts));
          }
          else
          {
            //assign direct
            DA.SetData(1, _pts);
          }
        }
      }
      else
      {
        DA.SetData(1, null);
      }
      if (_polyline != null)
      {
        if (GH_Format.TreatAsCollection(_polyline))
        {
          IEnumerable __enum__polyline = (IEnumerable)(_polyline);
          DA.SetDataList(2, __enum__polyline);
        }
        else
        {
          if (_polyline is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(2, (Grasshopper.Kernel.Data.IGH_DataTree)(_polyline));
          }
          else
          {
            //assign direct
            DA.SetData(2, _polyline);
          }
        }
      }
      else
      {
        DA.SetData(2, null);
      }
      if (_text != null)
      {
        if (GH_Format.TreatAsCollection(_text))
        {
          IEnumerable __enum__text = (IEnumerable)(_text);
          DA.SetDataList(3, __enum__text);
        }
        else
        {
          if (_text is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(3, (Grasshopper.Kernel.Data.IGH_DataTree)(_text));
          }
          else
          {
            //assign direct
            DA.SetData(3, _text);
          }
        }
      }
      else
      {
        DA.SetData(3, null);
      }
      if (_length != null)
      {
        if (GH_Format.TreatAsCollection(_length))
        {
          IEnumerable __enum__length = (IEnumerable)(_length);
          DA.SetDataList(4, __enum__length);
        }
        else
        {
          if (_length is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(4, (Grasshopper.Kernel.Data.IGH_DataTree)(_length));
          }
          else
          {
            //assign direct
            DA.SetData(4, _length);
          }
        }
      }
      else
      {
        DA.SetData(4, null);
      }
      if (_timecost != null)
      {
        if (GH_Format.TreatAsCollection(_timecost))
        {
          IEnumerable __enum__timecost = (IEnumerable)(_timecost);
          DA.SetDataList(5, __enum__timecost);
        }
        else
        {
          if (_timecost is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(5, (Grasshopper.Kernel.Data.IGH_DataTree)(_timecost));
          }
          else
          {
            //assign direct
            DA.SetData(5, _timecost);
          }
        }
      }
      else
      {
        DA.SetData(5, null);
      }

    }
    catch (Exception ex)
    {
      this.__err.Add(string.Format("Script exception: {0}", ex.Message));
    }
    finally
    {
      //Add errors and messages... 
      if (owner.Params.Output.Count > 0)
      {
        if (owner.Params.Output[0] is Grasshopper.Kernel.Parameters.Param_String)
        {
          List<string> __errors_plus_messages = new List<string>();
          if (this.__err != null) { __errors_plus_messages.AddRange(this.__err); }
          if (this.__out != null) { __errors_plus_messages.AddRange(this.__out); }
          if (__errors_plus_messages.Count > 0) 
            DA.SetDataList(0, __errors_plus_messages);
        }
      }
    }
  }
}