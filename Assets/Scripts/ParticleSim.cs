using UnityEngine;
using System.Collections;

/* 
 * HandMadeHero Particle test with Unity
 * Tutorial from HandMadeHero broadcast: https://www.youtube.com/watch?v=G6OGKP3MaUI
 * */

public class ParticleSim : MonoBehaviour {

	const int size = 128;
    const int Particle_Cel_Dim = 16;
	int NextParticle=0;
	particle[] particles = new particle[size];
    particle_cel[,] ParticleCels = new particle_cel[Particle_Cel_Dim, Particle_Cel_Dim];

	public Transform sprite;
	SpriteRenderer[] sprites;


	void Start () {
	
		sprites = new SpriteRenderer[size];

		for (int i = 0; i < sprites.Length; i++) 
		{
			var clone = Instantiate(sprite,Vector3.one*999,Quaternion.identity) as Transform;
			sprites[i] = clone.GetComponent<SpriteRenderer>();
		}
	}
	

	void Update () {

        // grid simulation

        // reset array
        ParticleCels = new particle_cel[Particle_Cel_Dim, Particle_Cel_Dim];
        /*
        for (int y = 0; y < gridSize; ++y)
        {
            for (int x = 0; x < gridSize; ++x)
            {
                ParticleCels[y, x].Density = 0;
                ParticleCels[y, x].VelocityTimesDensity = Vector3.zero;
            }
        }*/

        // spawn particle
		for (int ParticleSpawnIndex = 0; ParticleSpawnIndex < 1; ++ParticleSpawnIndex)
		{
			NextParticle=(NextParticle+1) % size;

			//Debug.Log(NextParticle);

            particles[NextParticle].P = new Vector3(Random.Range(-0.1f, 0.1f), 0f, 0);
			particles[NextParticle].dP = new Vector3(Random.Range(-0.15f,0.15f),0.7f,0)*7;
            particles[NextParticle].ddP = new Vector3(0f, -9.81f, 0f); // gravity
			particles[NextParticle].Col = new Vector4(1,1,0,2);
            particles[NextParticle].dCol = new Vector4(0, Random.Range(-0.5f, -1.5f), 0, Random.Range(-0.5f, -2f));

            sprites[NextParticle].transform.position = Vector3.zero;
            sprites[NextParticle].transform.localScale = Vector3.one;
           //sprites[NextParticle].GetComponent<Rigidbody2D>().velocity = Vector2.zero;

		}

        // grid sim
        float GridScale = 0.2f;
        float InvGridScale = 1f/GridScale;
        Vector3 GridOrigin = new Vector3(-0.5f *GridScale * Particle_Cel_Dim, 0f, 0f);

        for (int ParticleIndex = 0; ParticleIndex < particles.Length; ++ParticleIndex)
        {

            Vector3 P = InvGridScale * (particles[ParticleIndex].P - GridOrigin);

            int X = (int)P.x;
            int Y = (int)P.y;

            if (X < 0) X = 0;
            if (X > Particle_Cel_Dim-1) X = Particle_Cel_Dim-1;
            if (Y < 0) Y = 0;
            if (Y > Particle_Cel_Dim - 1) Y = Particle_Cel_Dim - 1;

            float Density = 1f;

            ParticleCels[Y, X].Density += Density;
            ParticleCels[Y, X].VelocityTimesDensity += Density * particles[ParticleIndex].dP;
        }

        // grid preview
        for (int y = 0; y < Particle_Cel_Dim; ++y)
        {
            for (int x = 0; x < Particle_Cel_Dim; ++x)
            {

                float Density = ParticleCels[y, x].Density;
                Color cellColor = new Color(Density,Density,Density,1);

                Vector3 gridPos = GridScale*new Vector3(x, y, 0f) + GridOrigin;

                Debug.DrawLine(gridPos,gridPos+new Vector3(1,0,0),cellColor);
                Debug.DrawLine(gridPos, gridPos + new Vector3(0, 1, 0), cellColor);
            }
        }


        // simulate
		for (int ParticleIndex = 0; ParticleIndex < particles.Length; ++ParticleIndex)
		{
			// calculate movement
            particles[ParticleIndex].P += (0.5f * Mathf.Sqrt(Time.deltaTime) * Time.deltaTime * particles[NextParticle].ddP + Time.deltaTime * particles[ParticleIndex].dP);
            particles[ParticleIndex].dP += Time.deltaTime * particles[ParticleIndex].ddP;
			particles[ParticleIndex].Col +=  Time.deltaTime*particles[ParticleIndex].dCol;

            // ground collision
//            Debug.Log(particles[ParticleIndex].Col.y);
            if (particles[ParticleIndex].P.y<0f)
            {
                float CoefficientOfRestitution = 0.5f;
                particles[ParticleIndex].P.y = -particles[ParticleIndex].P.y;
                particles[ParticleIndex].dP.y = -CoefficientOfRestitution*particles[ParticleIndex].dP.y;
            }

			// clamp colors
			particles[ParticleIndex].Col.x = Clamp01(particles[ParticleIndex].Col.x);
			particles[ParticleIndex].Col.y = Clamp01(particles[ParticleIndex].Col.y);
			particles[ParticleIndex].Col.z = Clamp01(particles[ParticleIndex].Col.z);
			particles[ParticleIndex].Col.w = Clamp01(particles[ParticleIndex].Col.w);

            // fix particle sudden spawn by making it transparent first, FIXME broken
            if (particles[ParticleIndex].Col.w>0.9f)
            {
                particles[ParticleIndex].Col.w = 0.9f*Clamp01MapToRange(1f,(particles[ParticleIndex].Col.w),0.9f);
            }


			// set values
			sprites[ParticleIndex].transform.position = particles[ParticleIndex].P;
			sprites[ParticleIndex].color = (Color)particles[ParticleIndex].Col;

            var s = particles[ParticleIndex].Col.w;
            sprites[ParticleIndex].transform.localScale = new Vector3(s, s, s);
		}
	} // Update()


    // helper functions
    float Clamp01MapToRange(float Min, float t, float Max)
    {

        // temporary fix for negative values
        float tempMin = Min;
        Min = Mathf.Min(Min, Max);
        Max = Mathf.Max(tempMin, Max);


        float Result = 0f;
        float Range = Max - Min;

        //Debug.Log(Range);

        if (Range != 0f)
        {
            Result = Clamp01((t - Min) / Range);
        }

        return Result;
    }

    float Clamp01(float Value)
    {
        float Result = Clamp(0f, Value, 1f);

        return Result;
    }

    float Clamp(float Min, float Value, float Max)
    {
        float Result = Value;

        if (Result<Min)
        {
            Result = Min;
        }else if (Result>Max)
        {
            Result = Max;
        }

        return Result;
    }

}
