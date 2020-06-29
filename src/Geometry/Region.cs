// https://strusoft.com/

using System.Collections.Generic;
using System.Xml.Serialization;

#region dynamo
using Autodesk.DesignScript.Runtime;
#endregion

namespace FemDesign.Geometry
{
    /// <summary>
    /// region_type
    /// 
    /// Surfaces in FEM-Design are expressed as regions of contours (outlines). This extended region also contains a LCS to keep track of directions.
    /// </summary>
    [System.Serializable]
    [IsVisibleInDynamoLibrary(false)]
    public class Region
    {
        [XmlIgnore]
        internal Geometry.FdCoordinateSystem coordinateSystem { get; set; }
        [XmlElement("contour")]
        public List<Contour> contours = new List<Contour>(); // sequence: contour_type

        /// <summary>
        /// Parameterless constructor for serialization.
        /// </summary>
        internal Region()
        {
            
        }

        internal Region(List<Contour> contours)
        {
            this.contours = contours;
        }

        internal Region(List<Contour> _contours, FdCoordinateSystem _coordinateSystem)
        {
            this.contours = _contours;
            this.coordinateSystem = _coordinateSystem;
        }
        
        /// <summary>
        /// Get region from a Slab.
        /// </summary>
        internal static Region FromSlab(Shells.Slab slab)
        {
            return slab.slabPart.region;
        }

        /// <summary>
        /// Set EdgeConnection on Edge in region by index.
        /// </summary>
        internal void SetEdgeConnection(Shells.ShellEdgeConnection edgeConnection, int index)
        {
            if (edgeConnection.release)
            {
                int edgeIdx = 0;
                foreach (Contour contour in this.contours)
                {
                    if (contour.edges != null)
                    {
                        int cInstance = 0;
                        foreach (Edge edge in contour.edges)
                        {
                            cInstance++;
                            if (index == edgeIdx)
                            {
                                string name = "CE." + cInstance.ToString();
                                Shells.ShellEdgeConnection ec = Shells.ShellEdgeConnection.CopyExisting(edgeConnection, name);
                                edge.edgeConnection = ec;
                                return;
                            }
                            edgeIdx++;
                        }
                    }
                    else
                    {
                        throw new System.ArgumentException("No edges in contour!");
                    }
                }
            }
            else
            {
                // don't modify edges if no release on edgeConnection.
            }

            // edge not found
            throw new System.ArgumentException("Edge not found.");
        }

        /// <summary>
        /// Set EdgeConnection on all Edges in Region.
        /// </summary>
        internal void SetEdgeConnections(Shells.ShellEdgeConnection edgeConnection)
        {
            if (edgeConnection.release)
            {
                foreach (Contour contour in this.contours)
                {
                    if (contour.edges != null)
                    {
                        int cInstance = 0;
                        foreach (Edge edge in contour.edges)
                        {
                            cInstance++;
                            string name = "CE." + cInstance.ToString();
                            Shells.ShellEdgeConnection ec = Shells.ShellEdgeConnection.CopyExisting(edgeConnection, name);
                            edge.edgeConnection = ec;
                        }
                    }
                    else
                    {
                        throw new System.ArgumentException("No edges in contour!");
                    }
                }
            }
            else
            {
                // don't modify edges if no releases on edgeConnection.
            }
        }

        /// <summary>
        /// Get all EdgeConnection from all Edges in Region.
        /// </summary>
        internal List<Shells.ShellEdgeConnection> GetEdgeConnections()
        {
            var edgeConnections = new List<Shells.ShellEdgeConnection>();
            foreach (Contour contour in this.contours)
            {
                foreach (Edge edge in contour.edges)
                {
                    edgeConnections.Add(edge.edgeConnection);
                }
            }
            return edgeConnections;
        }

        /// <summary>
        /// Returns a new instance of region, without any EdgeConnections.
        /// </summary>
        /// <returns></returns>
        internal Region RemoveEdgeConnections()
        {
            Region newRegion = this.DeepClone();
            foreach (Contour newContour in newRegion.contours)
            {
                foreach (Edge newEdge in newContour.edges)
                {
                    if (newEdge.edgeConnection != null)
                    {
                        newEdge.edgeConnection = null;
                    }
                }
            }
            return newRegion;
        }

        #region dynamo
        /// <summary>
        /// Create Region from Dynamo surface.
        /// </summary>
        public static Geometry.Region FromDynamo(Autodesk.DesignScript.Geometry.Surface obj)
        {
            // get all perimeter curves
            // curves[] is ordered by the loops of the surface.
            // the assumption here is that the loop with the largest circumference (i.e. the outline) is placed first in the array
            // for fd definition it is neccessary that the outline is the first contour, the subsequent loops can have any order.
            Autodesk.DesignScript.Geometry.Curve[] curves = obj.PerimeterCurves();

            // find closed outlines
            List<Autodesk.DesignScript.Geometry.Curve> perimeterCurves = new List<Autodesk.DesignScript.Geometry.Curve>();
            List<List<Autodesk.DesignScript.Geometry.Curve>> container = new List<List<Autodesk.DesignScript.Geometry.Curve>>();

            Autodesk.DesignScript.Geometry.Point pA0, pA1, pB0, pB1;
            // control if new perimeter
            // this happens is pA0/pA1 != pB0/pB1
            for (int idx = 0; idx < curves.Length; idx++)
            {
                if (idx == 0)
                {
                    perimeterCurves.Add(curves[idx]);
                }

                else
                {
                    pA0 = curves[idx - 1].StartPoint;
                    pA1 = curves[idx - 1].EndPoint;
                    pB0 = curves[idx].StartPoint;
                    pB1 = curves[idx].EndPoint;

                    // using Autodesk.DesignScript.Geometry.Point.Equals causes tolerance errors
                    // instead Autodesk.DesignScript.Geometry.Point.IsAlmostEqualTo is used
                    // note that this can cause errors as it is unclear what tolerance IsAlmostEqualTo uses
                    // another alternative would be to create an extension method, Equal(Point point, double tolerance), to Autodesk.DesignScript.Geometry.Point
                    if (pA0.IsAlmostEqualTo(pB0) || pA0.IsAlmostEqualTo(pB1) || pA1.IsAlmostEqualTo(pB0) || pA1.IsAlmostEqualTo(pB1))
                    { 
                        perimeterCurves.Add(curves[idx]);
                    }

                    // new perimeter
                    else
                    {
                        container.Add(new List<Autodesk.DesignScript.Geometry.Curve>(perimeterCurves));
                        perimeterCurves.Clear();
                        perimeterCurves.Add(curves[idx]);
                    }
                }
            }
            // add last perimeter to container
            container.Add(new List<Autodesk.DesignScript.Geometry.Curve>(perimeterCurves));
            perimeterCurves.Clear();

            // control if direction is consistent
            // as FromRhinoBrep.
            foreach (List<Autodesk.DesignScript.Geometry.Curve> items in container)
            {
                // if Contour consists of one curve.
                if (items.Count == 1)
                {
                    // check if curve is a Circle

                    // if curve is not a Circle, raise error.
                }

                // if Contour consists of more than one curve.
                else
                {
                    // using Autodesk.DesignScript.Geometry.Point.Equals causes tolerance errors
                    // instead Autodesk.DesignScript.Geometry.Point.IsAlmostEqualTo is used
                    // note that this can cause errors as it is unclear what tolerance IsAlmostEqualTo uses
                    // another alternative would be to create an extension method, Equal(Point point, double tolerance), to Autodesk.DesignScript.Geometry.Point
                    for (int idx = 0; idx < items.Count - 1; idx++)
                    {
                        // curve a = items[idx]
                        // curve b = items[idx + 1]
                        pA0 = items[idx].StartPoint;
                        pA1 = items[idx].EndPoint;
                        pB0 = items[idx + 1].StartPoint;
                        pB1 = items[idx + 1].EndPoint;

                        if (pA0.IsAlmostEqualTo(pB0))
                        {
                            if (idx == 0)
                            {
                                items[idx] = items[idx].Reverse();
                            }
                            else
                            {
                                throw new System.ArgumentException("pA0 == pB0 even though idx != 0. Bad outline.");
                            }
                        }

                        else if (pA0.IsAlmostEqualTo(pB1))
                        {
                            if (idx == 0)
                            {
                                items[idx] = items[idx].Reverse();
                                items[idx + 1] = items[idx + 1].Reverse();
                            }
                            else
                            {
                                throw new System.ArgumentException("pA0 == pB1 even though idx != 0. Bad outline.");
                            }
                        }

                        else if (pA1.IsAlmostEqualTo(pB0))
                        {
                            // pass
                        }

                        else if (pA1.IsAlmostEqualTo(pB1))
                        {
                            items[idx + 1] = items[idx + 1].Reverse();
                        }

                        else
                        {
                            throw new System.ArgumentException("Can't close outline. Bad outline.");
                        }
                    }
                }
            }

            // create contours
            List<Geometry.Edge> edges = new List<Geometry.Edge>();
            List<Geometry.Contour> contours = new List<Geometry.Contour>();

            foreach (List<Autodesk.DesignScript.Geometry.Curve> items in container)
            {
                foreach (Autodesk.DesignScript.Geometry.Curve curve in items)
                {
                    edges.Add(Geometry.Edge.FromDynamo(curve));
                }
                contours.Add(new Geometry.Contour(new List<Edge>(edges)));
                edges.Clear();
            }

            // get LCS
            FdCoordinateSystem cs = FdCoordinateSystem.FromDynamoSurface(obj);

            // return
            return new Geometry.Region(contours, cs);
        }

        /// <summary>
        /// Convert Region to Dynamo surface.
        /// </summary>
        public Autodesk.DesignScript.Geometry.Surface ToDynamoSurface()
        {
            // get closed curves
            List<Autodesk.DesignScript.Geometry.PolyCurve> closedCurves = new List<Autodesk.DesignScript.Geometry.PolyCurve>();
            foreach (Geometry.Contour contour in this.contours)
            {
                List<Autodesk.DesignScript.Geometry.Curve> curves = new List<Autodesk.DesignScript.Geometry.Curve>();
                foreach (Geometry.Edge edge in contour.edges)
                {
                    curves.Add(edge.ToDynamo());
                }
                closedCurves.Add(Autodesk.DesignScript.Geometry.PolyCurve.ByJoinedCurves(curves));
                curves.Clear();
            }

            // get surface
            List<Autodesk.DesignScript.Geometry.Surface> surfaces = new List<Autodesk.DesignScript.Geometry.Surface>();
            foreach (Autodesk.DesignScript.Geometry.PolyCurve closedCurve in closedCurves)
            {
                surfaces.Add(Autodesk.DesignScript.Geometry.Surface.ByPatch(closedCurve));
            }
            Autodesk.DesignScript.Geometry.Surface primarySurface = surfaces[0];
            surfaces.RemoveAt(0);
            foreach (Autodesk.DesignScript.Geometry.Surface secondarySurface in surfaces)
            {
                primarySurface = (Autodesk.DesignScript.Geometry.Surface)primarySurface.Split(secondarySurface)[0];
            }

            // return
            return primarySurface;
        }

        /// <summary>
        /// Convert Edges in Region to Dynamo curves.
        /// </summary>
        public List<List<Autodesk.DesignScript.Geometry.Curve>> ToDynamoCurves()
        {
            List<List<Autodesk.DesignScript.Geometry.Curve>> outlines = new List<List<Autodesk.DesignScript.Geometry.Curve>>();
            foreach (Geometry.Contour contour in this.contours)
            {
                List<Autodesk.DesignScript.Geometry.Curve> curves = new List<Autodesk.DesignScript.Geometry.Curve>();
                foreach (Geometry.Edge edge in contour.edges)
                {
                    curves.Add(edge.ToDynamo());
                }
                outlines.Add(new List<Autodesk.DesignScript.Geometry.Curve>(curves));
                curves.Clear();
            }
            return outlines;
        }

        #endregion

        #region grasshopper
        /// <summary>
        /// Create Region from Rhino brep.
        /// </summary>
        public static Geometry.Region FromRhino(Rhino.Geometry.Brep obj)
        {
            // check if brep contains more than 1 surface
            if (obj.Surfaces.Count != 1)
            {
                throw new System.ArgumentException("Brep contains more than 1 surface.");
            }

            // check if brep surface is planar
            if (!obj.Surfaces[0].IsPlanar())
            {
                throw new System.ArgumentException("Brep surface is not planar.");
            }

            // get outline curves
            var container = new List<List<Rhino.Geometry.Curve>>();
            var loopCurves = new List<Rhino.Geometry.Curve>();

            foreach (Rhino.Geometry.BrepLoop loop in obj.Loops)
            {
                foreach (Rhino.Geometry.BrepTrim trim in loop.Trims)
                {
                    loopCurves.Add(trim.Edge.EdgeCurve);
                }
                container.Add(new List<Rhino.Geometry.Curve>(loopCurves));
                loopCurves.Clear();
            }

            // Change direction of EdgeCurves if necessary.
            // Circular Edge in Contour (id est the only Edge in that Contour) should have a normal in the opposite direction from any other type of Contour in Region (id est the direction of the Contour should be opposite).
            // EndPoint of EdgeCurve[idx] should be equal to StartPoint of EdgeCurve[idx + 1] in a Contour.
            foreach (List<Rhino.Geometry.Curve> items in container)
            {
                // if Contour consists of one curve
                if (items.Count == 1)
                {
                    // check if curve is a Circle
                    Rhino.Geometry.Curve curve = items[0];
                    if (curve.IsArc() && curve.IsClosed)
                    {
                        // check if Circle is planar
                        if (curve.IsPlanar())
                        {
                            curve.TryGetPlane(out Rhino.Geometry.Plane plane);
                            // compare Contour and Surface normals
                            if (obj.Surfaces[0].NormalAt(0,0).IsParallelTo(plane.Normal, Tolerance.point3d) == 1)
                            {
                                // reverse direction of Circle
                                curve.Reverse();
                            }
                        }
                        else
                        {
                            throw new System.ArgumentException("Curve is not planar");
                        }
                    }

                    // if curve for some reason is not a Circle
                    else
                    {
                        // if the curve can not be represented by a circle then direction is irrelevant.
                        // pass
                    }
                }

                // if Contour consists of more than one curve (i.e. is not a Circle)
                else
                { 
                    Rhino.Geometry.Point3d pA0, pA1, pB0, pB1;
                    for (int idx = 0; idx < items.Count - 1; idx++)
                    {
                        // curve a = items[idx]
                        // curve b = items[idx + 1]
                        pA0 = items[idx].PointAtStart;
                        pA1 = items[idx].PointAtEnd;
                        pB0 = items[idx + 1].PointAtStart;
                        pB1 = items[idx + 1].PointAtEnd;

                        if (pA0.EpsilonEquals(pB0, Tolerance.point3d))
                        {
                            if (idx == 0)
                            {
                                items[idx].Reverse();
                            }
                            else
                            {
                                throw new System.ArgumentException("pA0 == pB0 even though idx != 0. Bad outline.");
                            }
                        }

                        else if (pA0.EpsilonEquals(pB1, Tolerance.point3d))
                        {
                            if (idx == 0)
                            {
                                items[idx].Reverse();
                                items[idx + 1].Reverse();
                            }
                            else
                            {
                                throw new System.ArgumentException("pA0 == pB1 even though idx != 0. Bad outline.");
                            }
                        }

                        else if (pA1.EpsilonEquals(pB0, Tolerance.point3d))
                        {
                            // pass
                        }

                        else if (pA1.EpsilonEquals(pB1, Tolerance.point3d))
                        {
                            items[idx + 1].Reverse();
                        }

                        else
                        {
                            throw new System.ArgumentException("Can't close outline. Bad outline.");
                        }
                    }

                    // check if outline is closed.
                    pA1 = items[items.Count - 1].PointAtEnd;
                    pB0 = items[0].PointAtStart;
                    if (pA1.EpsilonEquals(pB0, Tolerance.point3d))
                    {
                        
                    }
                    
                    else
                    {
                        throw new System.ArgumentException("Can't close outline. Bad outline.");
                    }
                }
            }

            // Create contours
            List<Geometry.Edge> edges = new List<Geometry.Edge>();
            List<Geometry.Contour> contours = new List<Geometry.Contour>();

            foreach (List<Rhino.Geometry.Curve> items in container)
            {
                foreach (Rhino.Geometry.Curve curve in items)
                {
                    foreach (Geometry.Edge edge in Geometry.Edge.FromRhinoBrep(curve))
                    {
                        edges.Add(edge);
                    }
                }
                contours.Add(new Geometry.Contour(new List<Edge>(edges)));
                edges.Clear();
            }

            // Get LCS
            FdCoordinateSystem cs = FdCoordinateSystem.FromRhinoSurface(obj.Surfaces[0]);

            // return
            return new Geometry.Region(contours, cs);
        }

        /// <summary>
        /// Convert Region to Rhino brep.
        /// </summary>
        public Rhino.Geometry.Brep ToRhinoBrep()
        {
            List<Rhino.Geometry.Curve> curves = new List<Rhino.Geometry.Curve>();
            foreach (Geometry.Contour contour in this.contours)
            {
                foreach (Geometry.Edge edge in contour.edges)
                {
                    curves.Add(edge.ToRhino());
                }
            }
            Rhino.Geometry.Brep[] breps = Rhino.Geometry.Brep.CreatePlanarBreps(curves);

            if (breps.Length == 1)
            {
                return breps[0];
            }

            else
            {
                throw new System.ArgumentException("More than one Brep was generated from Region.");
            }
        }

        /// <summary>
        /// Convert Edges in Region to Rhino curves.
        /// </summary>
        public List<Rhino.Geometry.Curve> ToRhinoCurves()
        {
            List<Rhino.Geometry.Curve> curves = new List<Rhino.Geometry.Curve>();
            foreach (Geometry.Contour contour in this.contours)
            {
                foreach (Geometry.Edge edge in contour.edges)
                {
                    curves.Add(edge.ToRhino());
                }
            }
            return curves;
        } 

        #endregion
    }
}