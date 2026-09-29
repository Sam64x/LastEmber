using System;
using System.Collections.Generic;
using Godot;
namespace LastEmber;

public sealed class DungeonGenerator
{
    public DungeonGraph Generate(ulong seed, IReadOnlyList<DungeonDefinition> definitions, int? selected=null)
    {
        if(definitions.Count==0)throw new ArgumentException("Dungeon catalog is empty.");
        using var rng=new RandomNumberGenerator {Seed=seed};
        int index=selected??rng.RandiRange(0,definitions.Count-1);
        if(index<0||index>=definitions.Count)throw new ArgumentOutOfRangeException(nameof(selected));
        var definition=definitions[index];
        var graph=DungeonGraph.Linear(seed,definition,DungeonDirector.MvpRoute.Length);
        var director=new DungeonDirector();director.Assign(graph);
        var catalog=definition.Geometries.Count>0?new List<RoomDefinition>(definition.Geometries).ToArray():RoomDefinition.CreateCatalog();
        var bag=new List<RoomDefinition>();
        RoomDefinition? previous=null;
        foreach(var room in graph.Rooms)
        {
            if(bag.Count==0)bag.AddRange(catalog);
            int pick=rng.RandiRange(0,bag.Count-1);
            if(bag.Count>1&&bag[pick]==previous)pick=(pick+1)%bag.Count;
            // The altar interaction and artwork share the center socket.
            if(room.Type==RoomType.Altar)
            {
                int safe=bag.FindIndex(g=>!System.Linq.Enumerable.Any(g.Solids,r=>r.Grow(110).HasPoint(g.Bounds.GetCenter())));
                if(safe>=0)pick=safe;
                else
                {
                    var sanctuary=Array.Find(catalog,g=>!System.Linq.Enumerable.Any(g.Solids,r=>r.Grow(110).HasPoint(g.Bounds.GetCenter())));
                    if(sanctuary==null)throw new InvalidOperationException("Room catalog needs a clear altar center.");
                    bag.Add(sanctuary);pick=bag.Count-1;
                }
            }
            room.Geometry=bag[pick];previous=room.Geometry;bag.RemoveAt(pick);
            if(room.Type==RoomType.Boss)
                room.Geometry=new RoomDefinition {Id="guardian-arena",Bounds=Room.Interior};
            room.Encounter=director.Choose(room,rng);
            room.Seed=((ulong)rng.Randi()<<32)|rng.Randi();
            room.PropVariant=rng.RandiRange(0,2);
        }
        return graph;
    }
}
