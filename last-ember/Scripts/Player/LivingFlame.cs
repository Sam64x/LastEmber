using Godot;
namespace LastEmber;

// GPU fire field runs at render cadence; its clock is supplied by the paused rig.
public partial class LivingFlame : Node2D
{
    private ShaderMaterial _flame=null!;
    private ArrayMesh _mesh=null!;
    public override void _Ready()
    {
        _flame=new ShaderMaterial {Shader=ResourceLoader.Load<Shader>("res://Assets/Shaders/living_ember.gdshader")};
        Material=_flame;
        const int columns=12,rows=16;
        var vertices=new Vector2[(columns+1)*(rows+1)];
        var uvs=new Vector2[vertices.Length];
        var indices=new int[columns*rows*6];int index=0;
        for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
        {
            int i=y*(columns+1)+x;uvs[i]=new Vector2(x/(float)columns,y/(float)rows);
            vertices[i]=new Vector2(-40,-58)+uvs[i]*new Vector2(80,86);
            if(x==columns||y==rows)continue;
            indices[index++]=i;indices[index++]=i+1;indices[index++]=i+columns+1;
            indices[index++]=i+1;indices[index++]=i+columns+2;indices[index++]=i+columns+1;
        }
        var arrays=new Godot.Collections.Array();arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex]=vertices;arrays[(int)Mesh.ArrayType.TexUV]=uvs;
        arrays[(int)Mesh.ArrayType.Index]=indices;
        _mesh=new ArrayMesh();_mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);
    }
    public void Update(float time,float blue,float life,Vector2 airflow,float surge,float movement=0,float turn=0,Vector2 dashDirection=default,float dashBlend=0)
    {
        _flame.SetShaderParameter("flame_time",time);
        _flame.SetShaderParameter("blue_blend",blue);
        _flame.SetShaderParameter("life",life);
        _flame.SetShaderParameter("airflow",airflow);
        _flame.SetShaderParameter("surge",surge);
        _flame.SetShaderParameter("movement",movement);
        _flame.SetShaderParameter("turn",turn);
        _flame.SetShaderParameter("dash_direction",dashDirection);
        _flame.SetShaderParameter("dash_blend",dashBlend);
    }
    public override void _Draw()=>DrawMesh(_mesh,null);
    public override void _ExitTree(){_mesh.Dispose();_flame.Dispose();}
}
