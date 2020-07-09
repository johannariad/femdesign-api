// https://strusoft.com/
using System;
using Grasshopper.Kernel;

namespace FemDesign.GH
{
    public class StoreyDeconstruct: GH_Component
    {
       public StoreyDeconstruct(): base("Storey.Deconstruct", "Deconstruct", "Deconstruct a storey element.", "FemDesign", "Deconstruct")
       {

       }
       protected override void RegisterInputParams(GH_InputParamManager pManager)
       {
           pManager.AddGenericParameter("Storey", "Storey", "Storey.", GH_ParamAccess.item);           
       } 
       protected override void RegisterOutputParams(GH_OutputParamManager pManager)
       {
           pManager.AddTextParameter("Guid", "Guid", "Guid.", GH_ParamAccess.item);
           pManager.AddPointParameter("Origo", "Origo", "Origo.", GH_ParamAccess.item);
           pManager.AddVectorParameter("Direction", "Direction" , "Direction.", GH_ParamAccess.item);
           pManager.AddNumberParameter("DimensionX", "DimensionX", "DimensionX.", GH_ParamAccess.item);
           pManager.AddNumberParameter("DimensionY", "DimensionY", "DimensionY.", GH_ParamAccess.item);
           pManager.AddTextParameter("Name", "Name", "Name.", GH_ParamAccess.item);
       }
       protected override void SolveInstance(IGH_DataAccess DA)
       {
            // get input
            FemDesign.StructureGrid.Storey obj = null;
            if (!DA.GetData(0, ref obj))
            {
                return;
            }
            if (obj == null)
            {
                return;
            }

            // return
            DA.SetData(0, obj.guid);
            DA.SetData(1, obj.origo.ToRhino());
            DA.SetData(2, obj.direction.ToRhino());
            DA.SetData(3, obj.dimensionX);
            DA.SetData(4, obj.dimensionY);
            DA.SetData(5, obj.name);
       }
       protected override System.Drawing.Bitmap Icon
       {
           get
           {
                return FemDesign.Properties.Resources.StoreyDeconstruct;
           }
       }
       public override Guid ComponentGuid
       {
           get { return new Guid("2a4592ee-9f88-484d-8409-9ae1efad88a6"); }
       }
    }
}