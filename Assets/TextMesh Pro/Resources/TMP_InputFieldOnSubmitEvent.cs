using TMPro;
using UnityEngine;

namespace TextToTMPNamespace
{
	/// <summary>
	/// This component is attached to upgraded InputField components with non-empty "On Submit" events. TMP_InputField component doesn't allow serializing
	/// the On Submit event so this component acts as a bridge for the On Submit event between legacy InputField and TMP_InputField.
	/// If you delete this script, any TMP_InputField with this component attached will have their On Submit event stop functioning (see: https://github.com/yasirkula/UnityTextToTextMeshProUpgradeTool#known-limitations)
	/// </summary>
	[DefaultExecutionOrder( -1000 )]
	[RequireComponent( typeof( TMP_InputField ) )]
	[DisallowMultipleComponent]
	[HelpURL( "https://github.com/yasirkula/UnityTextToTextMeshProUpgradeTool#known-limitations" )]
	public class TMP_InputFieldOnSubmitEvent : MonoBehaviour
	{
		public TMP_InputField.SubmitEvent onSubmit = new TMP_InputField.SubmitEvent();

		private void Awake()
		{
			GetComponent<TMP_InputField>().onSubmit = onSubmit;
		}
	}
}