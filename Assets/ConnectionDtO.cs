using UnityEngine;
using System.Collections;

public class ConnectionDtO
{
	
	public string name;
	public string version;

	public ConnectionDtO(string nm, string nv)
	{
		name = nm;
		version = nv;
	}

	public ConnectionDtO()
	{
		name = "";
		version = "";
	}
}
