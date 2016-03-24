﻿/* The basic component of scrolling list.
 * Note that the camera is at (0,0).
 */
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ListBox : MonoBehaviour
{
	public int listBoxID;	// Must be unique, and count from 0
	public Text content;		// The content of the list box

	public ListBox lastListBox;
	public ListBox nextListBox;

	private int numOfListBox;
	private int contentID;
	private bool isTouchingDevice;

	private Vector2 maxWorldPos;		// The maximum world position in the view of camera
	private Vector2 unitWorldPos;
	private Vector2 lowerBoundWorldPos;
	private Vector2 upperBoundWorldPos;
	private Vector2 rangeBoundWorldPos;

	private Vector3 slidingWorldPos;	// The sliding distance at each frame
	private Vector3 slidingWorldPosLeft;

	private Vector3 originalLocalScale;

	private bool keepSliding = false;
	private int slidingFrames;

	void Start()
	{
		numOfListBox = ListPositionCtrl.Instance.listBoxes.Length;

		maxWorldPos = ( Vector2 ) Camera.main.ScreenToWorldPoint(
			new Vector2( Camera.main.pixelWidth, Camera.main.pixelHeight ) );

		unitWorldPos = maxWorldPos / ListPositionCtrl.Instance.divideFactor;

		lowerBoundWorldPos = unitWorldPos * (float)( -1 * numOfListBox / 2 - 1 );
		upperBoundWorldPos = unitWorldPos * (float)( numOfListBox / 2 + 1 );
		rangeBoundWorldPos = unitWorldPos * (float)numOfListBox;

		originalLocalScale = transform.localScale;

		initialPosition( listBoxID );
		initialContent();
	}

	/* Initialize the content of ListBox.
	 */
	void initialContent()
	{
		if ( listBoxID == numOfListBox / 2 )
			contentID = 0;
		else if ( listBoxID < numOfListBox / 2 )
			contentID = ListBank.Instance.getListLength() - ( numOfListBox / 2 - listBoxID );
		else
			contentID = listBoxID - numOfListBox / 2;

		while ( contentID < 0 )
			contentID += ListBank.Instance.getListLength();
		contentID = contentID % ListBank.Instance.getListLength();

		updateContent( ListBank.Instance.getListContent( contentID ) );
	}

	void updateContent( string content )
	{
		this.content.text = content;
	}

	/* Make the list box slide for delta y position.
	 */
	public void setSlidingDistance( float distance )
	{
		keepSliding = true;
		slidingFrames = ListPositionCtrl.Instance.slidingFrames;

		slidingWorldPosLeft = new Vector3( 0.0f, distance, 0.0f );
		slidingWorldPos = Vector3.zero;
		slidingWorldPos.y = Mathf.Lerp( 0.0f, distance, ListPositionCtrl.Instance.slidingFactor );
	}

	/* Move the listBox for world position unit.
	 * Move up when "up" is true, or else, move down.
	 */
	public void unitMove( int unit, bool up )
	{
		float deltaPosY;

		if ( up )
			deltaPosY = unitWorldPos.y * (float)unit;
		else
			deltaPosY = unitWorldPos.y * (float)unit * -1;

		setSlidingDistance( deltaPosY );
	}

	void Update()
	{
		if ( keepSliding )
		{
			--slidingFrames;
			if ( slidingFrames == 0 )
			{
				keepSliding = false;
				// At the last sliding frame, move to that position.
				// At free moving mode, this function is disabled.
				if ( ListPositionCtrl.Instance.alignToCenter ||
				    ListPositionCtrl.Instance.controlByButton )
					updatePosition( slidingWorldPosLeft );
				return;
			}

			updatePosition( slidingWorldPos );
			slidingWorldPosLeft -= slidingWorldPos;
			slidingWorldPos.y = Mathf.Lerp( 0.0f, slidingWorldPosLeft.y, ListPositionCtrl.Instance.slidingFactor );
		}
	}

	/* Initialize the position of the list box accroding to its ID.
	 */
	void initialPosition( int listBoxID )
	{
		transform.position = new Vector3( 0.0f,
		                                 unitWorldPos.y * (float)( listBoxID * -1 + numOfListBox / 2 ),
		                                 0.0f );
		updateXPosition();
	}

	/* Update the position of ListBox accroding to the delta position at each frame.
	 */
	public void updatePosition( Vector3 deltaPosition )
	{
		transform.position += deltaPosition;
		updateXPosition();
		checkBoundary();
	}

	/* Calculate the x position accroding to the y position.
	 */
	void updateXPosition()
	{
		transform.position = new Vector3(
			maxWorldPos.x * ListPositionCtrl.Instance.x_pivot -
			maxWorldPos.x * ListPositionCtrl.Instance.angularity * Mathf.Cos( transform.position.y / upperBoundWorldPos.y * Mathf.PI / 2.0f ),
			transform.position.y,
			transform.position.z );
		updateSize();
	}

	/* Check if the ListBox is beyond the upper or lower bound or not.
	 * If does, move the ListBox to the other side.
	 */
	void checkBoundary()
	{
		float beyondWorldPosY = 0.0f;

		if ( transform.position.y < lowerBoundWorldPos.y )
		{
			beyondWorldPosY = ( lowerBoundWorldPos.y - transform.position.y ) % rangeBoundWorldPos.y;
			transform.position = new Vector3(
				transform.position.x,
				upperBoundWorldPos.y - unitWorldPos.y - beyondWorldPosY,
				transform.position.z );
			updateToLastContent();
		}
		else if ( transform.position.y > upperBoundWorldPos.y )
		{
			beyondWorldPosY = ( transform.position.y - upperBoundWorldPos.y ) % rangeBoundWorldPos.y;
			transform.position = new Vector3(
				transform.position.x,
				lowerBoundWorldPos.y + unitWorldPos.y + beyondWorldPosY,
				transform.position.z );
			updateToNextContent();
		}

		updateXPosition();
	}

	/* Scale the size of listBox accroding to the Y position.
	 */
	void updateSize()
	{
		transform.localScale = originalLocalScale *
			( 1.0f + ListPositionCtrl.Instance.scaleFactor * ( upperBoundWorldPos.y - Mathf.Abs( transform.position.y ) ) );
	}
	
	public int getCurrentContentID()
	{
		return contentID;
	}

	/* Update to the last content of the next ListBox
	 * when the ListBox appears at the top of camera.
	 */
	void updateToLastContent()
	{
		contentID = nextListBox.getCurrentContentID() - 1;
		contentID = ( contentID < 0 ) ? ListBank.Instance.getListLength() - 1 : contentID;

		updateContent( ListBank.Instance.getListContent( contentID ) );
	}

	/* Update to the next content of the last ListBox
	 * when the ListBox appears at the bottom of camera.
	 */
	void updateToNextContent()
	{
		contentID = lastListBox.getCurrentContentID() + 1;
		contentID = ( contentID == ListBank.Instance.getListLength() ) ? 0 : contentID;

		updateContent( ListBank.Instance.getListContent( contentID ) );
	}
}
