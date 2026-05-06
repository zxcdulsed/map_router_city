public static class MyEditorMenu
{
	[Menu("Editor", "map_router_city/My Menu Option")]
	public static void OpenMyMenu()
	{
		EditorUtility.DisplayDialog("It worked!", "This is being called from your library's editor code!");
	}
}
