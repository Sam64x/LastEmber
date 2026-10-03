extends SceneTree
func _initialize():
    call_deferred("render_asset")
func render_asset():
    root.add_child(load("res://Tools/ExecutionerArtPreview.cs").new())
