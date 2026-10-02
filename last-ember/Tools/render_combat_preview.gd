extends SceneTree

# Developer art entry point. Does not instantiate the game's main scene.
func _initialize():
    call_deferred("render_asset")
func render_asset():
    root.add_child(load("res://Tools/CombatArtPreview.cs").new())
