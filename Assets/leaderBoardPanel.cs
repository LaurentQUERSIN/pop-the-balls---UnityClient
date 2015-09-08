using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LeaderBoardPanel : MonoBehaviour {

	public Text firstTxt;
	public Text secTxt;
	public Text thirdTxt;
	public Text fourTxt;
	public Text fifthTxt;
	public Text firstNbr;
	public Text secNbr;
	public Text thirdNbr;
	public Text fourNbr;
	public Text fifthNbr;

	public Text localTxt;
	public Text localNbr;

	public void updateLeaderBoard(LeaderBoardDtO up)
	{
		firstTxt.text = up.names[0];
		secTxt.text = up.names[1];
		thirdTxt.text = up.names[2];
		fourTxt.text = up.names[3];
		fifthTxt.text = up.names[4];

		firstNbr.text = up.scores[0];
		secNbr.text = up.scores[1];
		thirdNbr.text = up.scores[2];
		fourNbr.text = up.scores[3];
		fifthNbr.text = up.scores[4];

		localNbr.text = up.localNbr;

	}
}

public class LeaderBoardDtO
{
	public string[] names;
	public string[] scores;

	public string localNbr;
}
