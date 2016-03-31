﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using RTree;
using System;

namespace Overkill
{
    public static class Util
    {
        public static IEntityProxy MakeProxy(Entity ent, Overkill.Options opts, RTree<DbEntity> tree)
        {
            if (ent is Line)
                return new LineProxy(ent as Line, opts, tree);
            if (ent is Polyline)
                return (ent as Polyline).Closed ?
                    (IEntityProxy)new ClosedPolylineProxy((Polyline) ent, opts, tree) : 
                    new PolylineProxy((Polyline) ent, opts, tree);
            return new EntityProxy(ent, opts, tree);
        }
            
        // Проверка двух примитивов на идентичность
        public static bool IsEqual(Entity ent1, Entity ent2)
        {
            ResultBuffer rb1 = AcadLib.ObjectARX._acdbEntGet(new UIntPtr((ulong)ent1.Id.OldIdPtr.ToInt64()));
            ResultBuffer rb2 = AcadLib.ObjectARX._acdbEntGet(new UIntPtr((ulong)ent2.Id.OldIdPtr.ToInt64()));
            TypedValue[] arr1 = rb1.AsArray();
            TypedValue[] arr2 = rb2.AsArray();
            if (arr1.Length != arr2.Length)
            {
                if (arr1.Length > arr2.Length)
                {
                    TypedValue[] tmp = arr1;
                    arr2 = arr1;
                    arr1 = tmp;
                }
                for (int i = 0; i < arr2.Length; i++)
                {
                    if (arr2[i].TypeCode == (short)DxfCode.ControlString)
                    {
                        int n = 0;
                        for (int j = i + 1; j<arr2.Length; j++)
                        {
                            if (arr2[j].TypeCode == (short)DxfCode.ControlString)
                            {
                                if (++n != 3)
                                    continue;
                                TypedValue[] arr2Copy = arr2;
                                arr2 = new TypedValue[arr2.Length-(j-i+1)];
                                for (int k=0; k<i; k++)
                                    arr2[k] = arr2Copy[k];
                                for (int k = j + 1; k < arr2Copy.Length; k++)
                                    arr2[k-j + i-1] = arr2Copy[k];
                                i = j;
                                break;
                            }
                        }
                    }
                }
                if (arr1.Length != arr2.Length)
                    return false;
            }
            for (int i = 1; i < arr1.Length; i++)
            {
                TypedValue v1 = arr1[i];
                TypedValue v2 = arr2[i];
                if (v1.TypeCode == (short)DxfCode.Handle || v1.TypeCode ==(short)DxfCode.AttributeTag || v1.TypeCode == (short)DxfCode.SoftPointerId)
                    continue;
                String str1 = v1.ToString();
                String str2 = v2.ToString();
                if (str1 != str2)
                    return false;
            }
            return true;
        }

        // Проверка двух линий на идентичность
        public static bool IsEqual(Line l1, Line l2, double tolerance)
        {
            return l1.StartPoint.DistanceTo(l2.StartPoint) < tolerance &&
                   l1.EndPoint.DistanceTo(l2.StartPoint) < tolerance ||
                   l1.StartPoint.DistanceTo(l2.EndPoint) < tolerance &&
                   l1.EndPoint.DistanceTo(l2.StartPoint) < tolerance;
        }

        private static Rectangle GetRect(Point3d p1, Point3d p2)
        {
            return new Rectangle((float)p1.X, (float)p1.Y, (float)p2.X,
                (float)p2.Y, (float)p1.Z, (float)p2.Z);
        }

        public static Rectangle GetRect(Line l1)
        {
            return GetRect(l1.StartPoint, l1.EndPoint);
        }

        public static Rectangle GetRect(LineSegment3d l1)
        {
            return GetRect(l1.StartPoint, l1.EndPoint);
        }

        public static Rectangle GetRect(Extents3d ext)
        {
            return GetRect(ext.MinPoint, ext.MaxPoint);
        }

        public static Rectangle GetRect(Entity ent)
        {
            return GetRect(ent.GeometricExtents);
        }

        public static bool AreLinesParrallelAndItersects(Line l1, Line l2, double tolerance)
        {
            Vector3d vecL1 = l1.StartPoint.GetVectorTo(l1.EndPoint);
            Vector3d vecL2 = l2.StartPoint.GetVectorTo(l2.EndPoint);

            if (!vecL1.IsParallelTo(vecL2, new Tolerance(tolerance, tolerance)))
                return false;
            return IsPointLiesOnLine(l1, l2.StartPoint, tolerance) || IsPointLiesOnLine(l1, l2.EndPoint, tolerance);
        }

        // Проверка, лежит ли точка на отрезке
        public static bool IsPointLiesOnLine(Curve l1, Point3d startPoint, double tolerance)
        {
            Point3d p = l1.GetClosestPointTo(startPoint, false);
            return p.DistanceTo(startPoint) < tolerance;
        }

        public static bool IsPointLiesOnLine(Curve3d l1, Point3d startPoint, double tolerance)
        {
            return l1.GetDistanceTo(startPoint) < tolerance;
        }

    }
}