using System.Collections.Generic;
using UnityEngine;
using static AmnesiaUnity.Editor.SanRocco.Kit;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class QuarryLandform
    {
        public static void Create(Transform parent)
        {
            var rock=Mat("roccia_cava","#7D8582");
            var gravel=Mat("detrito_cava","#9A9D8D");
            float[] x={-30,-22,-14,-6,4,12,21,32};
            float[] z={-7,3,14,26,38,46};
            float[,] heights={
                {-.15f,6,7,7.5f,8,10,3,-.15f},
                {-.15f,8,11,12,14,15,9,-.15f},
                {-.15f,10,15,17,18,15,10,-.15f},
                {-.15f,8,13,16,15,12,8,-.15f},
                {-.15f,4,7,10,11,7,3,-.15f},
                {-.15f,-.15f,-.15f,-.15f,-.15f,-.15f,-.15f,-.15f}
            };
            var vertices=new List<Vector3>();var indices=new List<int>();
            for(int row=0;row<z.Length;row++) for(int col=0;col<x.Length;col++) {
                float offset=row>0&&row<z.Length-1&&col>0&&col<x.Length-1 ? Mathf.Sin(row*7+col*3)*1.4f : 0;
                vertices.Add(new Vector3(x[col]+offset,heights[row,col],z[row]+offset*.6f));
            }
            for(int row=0;row<z.Length-1;row++) for(int col=0;col<x.Length-1;col++) {
                int a=row*x.Length+col,b=a+x.Length;
                if((row+col)%2==0) indices.AddRange(new[]{a,b,a+1,a+1,b,b+1});
                else indices.AddRange(new[]{a,b,b+1,a,b+1,a+1});
            }
            Mesh(parent,"versante_roccioso",vertices.ToArray(),indices.ToArray(),rock);

            float[] cuts={-30,-22,-14,-12.7f,-7.3f,-6,-3.2f,3.2f,4,12,21,32};
            for(int i=0;i<cuts.Length-1;i++) {
                float a=cuts[i],b=cuts[i+1],middle=(a+b)/2;
                float bottom=middle>-12.7f&&middle<-7.3f?3.7f:middle>-3.2f&&middle<3.2f?3.25f:0;
                float ha=FrontHeight(a,x,heights),hb=FrontHeight(b,x,heights);
                Mesh(parent,"fronte_fratturato",new[]{
                    new Vector3(a,bottom,-7),new Vector3(b,bottom,-7),
                    new Vector3(a,ha,-7),new Vector3(b,hb,-7),
                    new Vector3(middle,(bottom+(ha+hb)/2)/2,-8-(i%3)*.35f)
                },new[]{0,4,1,0,2,4,2,3,4,4,3,1},i%4==0?gravel:rock);
            }

            // The extraction apron has an eroded outline rather than a concrete slab.
            vertices.Clear();indices.Clear();vertices.Add(new Vector3(0,.015f,-13));
            const int sides=15;
            for(int i=0;i<sides;i++) {
                float angle=i*Mathf.PI*2/sides;
                float radius=1+Mathf.Sin(i*4.1f)*.12f;
                vertices.Add(new Vector3(Mathf.Cos(angle)*22*radius,.015f,-13+Mathf.Sin(angle)*12*radius));
            }
            for(int i=0;i<sides;i++) indices.AddRange(new[]{0,(i+1)%sides+1,i+1});
            Mesh(parent,"piazzale_erosione",vertices.ToArray(),indices.ToArray(),gravel,false);

            // Angular fallen blocks cluster at the sides, clear of both tunnel mouths.
            for(int i=0;i<38;i++) {
                float sign=i%2==0?-1:1;
                float px=sign*(15+(i%5)*2.1f),pz=-14+(i/2)*.35f;
                float radius=.45f+(i%4)*.35f;
                var block=Cone(parent,"detrito_fratturato",new Vector3(px,.02f,pz),radius,.7f+(i%3)*.6f,i%3==0?gravel:rock,5,radius*.5f);
                block.transform.localRotation=Quaternion.Euler(0,i*47,0);
                block.AddComponent<MeshCollider>().sharedMesh=block.GetComponent<MeshFilter>().sharedMesh;
            }
            foreach(float px in new[]{-15.5f,-6.2f,5.7f}) {
                var flank=Cone(parent,"sperone_imbocco",new Vector3(px,0,-6),2.2f,5.5f,rock,5,1.2f);
                flank.AddComponent<MeshCollider>().sharedMesh=flank.GetComponent<MeshFilter>().sharedMesh;
            }
        }
        static float FrontHeight(float point,float[] x,float[,] heights)
        {
            for(int i=0;i<x.Length-1;i++) if(point<=x[i+1])
                return Mathf.Lerp(heights[0,i],heights[0,i+1],Mathf.InverseLerp(x[i],x[i+1],point));
            return heights[0,x.Length-1];
        }
    }
}
