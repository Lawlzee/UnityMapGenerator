using RoR2;
using UnityEngine;

namespace ProceduralStages
{
    public class GravZone : MonoBehaviour
    {
        public void OnTriggerEnter(Collider other)
        {
            ICharacterFlightParameterProvider component1 = other.GetComponent<ICharacterFlightParameterProvider>();
            if (component1 != null)
            {
                CharacterFlightParameters flightParameters = component1.flightParameters;
                --flightParameters.channeledFlightGranterCount;
                component1.flightParameters = flightParameters;
            }
            ICharacterGravityParameterProvider component2 = other.GetComponent<ICharacterGravityParameterProvider>();
            if (component2 == null)
                return;
            CharacterGravityParameters gravityParameters = component2.gravityParameters;
            --gravityParameters.environmentalAntiGravityGranterCount;
            component2.gravityParameters = gravityParameters;
        }

        public void OnTriggerExit(Collider other)
        {
            ICharacterGravityParameterProvider component1 = other.GetComponent<ICharacterGravityParameterProvider>();
            if (component1 != null)
            {
                CharacterGravityParameters gravityParameters = component1.gravityParameters;
                ++gravityParameters.environmentalAntiGravityGranterCount;
                component1.gravityParameters = gravityParameters;
            }
            ICharacterFlightParameterProvider component2 = other.GetComponent<ICharacterFlightParameterProvider>();
            if (component2 == null)
                return;
            CharacterFlightParameters flightParameters = component2.flightParameters;
            ++flightParameters.channeledFlightGranterCount;
            component2.flightParameters = flightParameters;
        }
    }
}
