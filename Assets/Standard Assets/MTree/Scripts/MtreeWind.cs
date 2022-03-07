using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class MtreeWind : MonoBehaviour {

    [Header("Global Windzone")]
    public WindZone windZone;
    [Header("Mtree Wind Offset")]
    public float windStrength = 0;
    public float windDirection = 0;
    public float windPulse = 0;
    public float windTurbulence = 0;
    [Range(0,1)]public float windRandomness = 1;
    [Header("Billboard")]
    public bool BillboardWind = false;
    [Range(0,1)]public float BillboardWindInfluence = .5f;

    // Updatecheck values
    float m_windStrength, m_windDirection, m_windPulse, m_windTurbulence;
    void Awake(){
        windZone = (WindZone)FindObjectOfType(typeof(WindZone));
        if(!windZone){
			windZone = (WindZone)gameObject.AddComponent (typeof(WindZone));
        }
    }

	void Update () {
        if(windZone){
            if(m_windStrength != windZone.windMain || m_windDirection != windZone.transform.rotation.eulerAngles.y || m_windPulse != windZone.windPulseFrequency || m_windTurbulence != windZone.windTurbulence){
                UpdateWindZone();
                m_windStrength = windZone.windMain;
                m_windDirection = windZone.transform.rotation.eulerAngles.y;
                m_windPulse = windZone.windPulseFrequency;
                m_windTurbulence = windZone.windTurbulence;
            }
        }
    }
	
    public void UpdateWind()
    {
        Shader.SetGlobalFloat("_WindStrength", windStrength);
        Shader.SetGlobalFloat("_WindDirection",windDirection);
        Shader.SetGlobalFloat("_WindPulse",windPulse);
        Shader.SetGlobalFloat("_WindTurbulence",windTurbulence);
    }
    public void UpdateWindZone()
    {
        Shader.SetGlobalFloat("_WindStrength",windZone.windMain + windStrength);
        Shader.SetGlobalFloat("_WindDirection",windZone.transform.localRotation.eulerAngles.y + windDirection);
        Shader.SetGlobalFloat("_WindPulse",windZone.windPulseFrequency + windPulse);
        Shader.SetGlobalFloat("_WindTurbulence",windZone.windTurbulence + windTurbulence);
    }
    public void OnValidate(){

        Shader.SetGlobalFloat("_RandomWindOffset",windRandomness);

        if(windZone)
            UpdateWindZone();
        if(!windZone)
            UpdateWind();

        if(BillboardWind){
            Shader.SetGlobalInt("BillboardWindEnabled",0);
            Shader.SetGlobalFloat("Billboard_WindStrength",BillboardWindInfluence);
            }
        if(!BillboardWind){
            Shader.SetGlobalInt("BillboardWindEnabled",1);
            }
        
    }
	public void ResetToZero(){
		windStrength = 0;
		windDirection = 0;
		windTurbulence = 0;
		windPulse = 0;
		UpdateWind ();
	}
	public void OnDisable(){
		ResetToZero();
	}
	public void OnDestroy(){
		ResetToZero ();
	}
	public void OnEnable(){
		if (windZone)
			UpdateWindZone ();
		else
			UpdateWind ();
	}
        
}
