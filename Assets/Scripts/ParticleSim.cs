using UnityEngine;
using System.Collections;

/* 
 * HandMadeHero Particle test with Unity
 * Tutorial from HandMadeHero broadcast: https://www.youtube.com/watch?v=G6OGKP3MaUI
 * Unity3D version by UnityCoder.com
 * */

public class ParticleSim : MonoBehaviour {

	bool debugMode = true;

	public Color initialColor = new Vector4(1,1,0,2);

	bool groundPlaneCollision = true;

	const int particlesAmount = 128;
	int spawnPerFrame = 1;

//	float maxSpeed = 1.5f;
//	public Transform[] obstacles;

    const int Particle_Cel_Dim = 64;
	int NextParticle=0;
	particle[] particles = new particle[particlesAmount];
    particle_cel[,] ParticleCels = new particle_cel[Particle_Cel_Dim, Particle_Cel_Dim];

	public Transform sprite;
	SpriteRenderer[] sprites;

	const float GridScale = 0.05f;
	float InvGridScale = 1f/GridScale;

	float groundY = 0;


	void Start () {
	
		sprites = new SpriteRenderer[particlesAmount];

		for (int i = 0; i < sprites.Length; i++) 
		{
			var clone = Instantiate(sprite,Vector3.one*999,Quaternion.identity) as Transform;
			sprites[i] = clone.GetComponent<SpriteRenderer>();
		}
	}
	

	void LateUpdate () 
	{

		Vector3 GridOrigin = new Vector3(-0.5f *GridScale * Particle_Cel_Dim, 0f, 0f);

        // reset array
       // ParticleCels = new particle_cel[Particle_Cel_Dim, Particle_Cel_Dim];      
		for (int y = 0; y < Particle_Cel_Dim; ++y)
        {
			for (int x = 0; x < Particle_Cel_Dim; ++x)
            {
				ParticleCels[y, x].Density = 0;
				//ParticleCels[y, x].VelocityTimesDensity = Vector3.zero;


				/*
				// borders test
				if (x==0 || y==0 || x==Particle_Cel_Dim-1 || y==Particle_Cel_Dim-1)
				{
	                ParticleCels[y, x].Density = 1;
				}else{
					ParticleCels[y, x].Density = 0;
				}*/

				/*
				// update obstacle maps test
				for (int i = 0; i < obstacles.Length; i++) 
				{
					float distance = (new Vector3(x,y,0)-(obstacles[i].position-GridOrigin)*InvGridScale).sqrMagnitude*2f;
					if (distance>Particle_Cel_Dim) distance=Particle_Cel_Dim;
					ParticleCels[y, x].Density += Mathf.Clamp((Particle_Cel_Dim-distance),0,Particle_Cel_Dim);
				}
 
				// test perlin map 
				ParticleCels[y, x].Density = Mathf.PerlinNoise(2.2f+x*0.06f,2.2f+y*0.06f);
				if (ParticleCels[y, x].Density<0.4f) ParticleCels[y, x].Density=0;
				if (ParticleCels[y, x].Density>0.4f) ParticleCels[y, x].Density*=9999f;
				*/

            }
        }

        // (re)spawn particle
		for (int ParticleSpawnIndex = 0; ParticleSpawnIndex < spawnPerFrame; ++ParticleSpawnIndex)
		{
			NextParticle=(NextParticle+1) % particlesAmount;

			// set initial values
			particles[NextParticle].P = transform.position+new Vector3(Random.Range(-0.05f, 0.05f), 0, 0); // start pos
//			particles[NextParticle].dP = new Vector3(Random.Range(-0.07f,0.07f),0.85f,0)*7; // start speed, global direction
			particles[NextParticle].dP = transform.TransformDirection(new Vector3(Random.Range(-0.07f,0.07f),0.85f,0)*7); // start speed in local transform direction
			particles[NextParticle].ddP = new Vector3(0f, -9.81f, 0f); // gravity
			particles[NextParticle].Col = initialColor;
            particles[NextParticle].dColor = new Vector4(0, Random.Range(-0.25f, -1.25f), 0, Random.Range(-0.25f, -1f));

            sprites[NextParticle].transform.position = Vector3.zero;
			sprites[NextParticle].transform.localScale = Vector3.one;
		}

        // map particles to grid & add cell density
        for (int ParticleIndex = 0; ParticleIndex < particles.Length; ++ParticleIndex)
        {
            Vector3 P = InvGridScale * (particles[ParticleIndex].P - GridOrigin);

            int X = (int)P.x;
            int Y = (int)P.y;

            if (X < 0) X = 0;
            if (X > Particle_Cel_Dim-1) X = Particle_Cel_Dim-1;
            if (Y < 0) Y = 0;
            if (Y > Particle_Cel_Dim - 1) Y = Particle_Cel_Dim - 1;

			float Density = particles[ParticleIndex].Col.w;

            ParticleCels[Y, X].Density += Density;
            ParticleCels[Y, X].VelocityTimesDensity += Density * particles[ParticleIndex].dP;
        }


		if (debugMode)
		{
			// grid preview
			for (int y = 0; y < Particle_Cel_Dim; ++y)
	        {
	            for (int x = 0; x < Particle_Cel_Dim; ++x)
	            {

	                float Alpha = Clamp01(0.1f*ParticleCels[y, x].Density);
	                Color cellColor = new Color(Alpha,Alpha,Alpha,1);

	                Vector3 gridPos = GridScale*new Vector3(x, y, 0f) + GridOrigin;

					Debug.DrawLine(gridPos,gridPos+new Vector3(1,0,0)*GridScale,cellColor);
					Debug.DrawLine(gridPos, gridPos + new Vector3(0, 1, 0)*GridScale, cellColor);
	            }
	        }
		}


        // simulate
		for (int ParticleIndex = 0; ParticleIndex < particles.Length; ++ParticleIndex)
		{
			// snap to grid
			Vector3 P = InvGridScale * (particles[ParticleIndex].P - GridOrigin);

			int X = (int)P.x;
			int Y = (int)P.y;

			if (X < 1) X = 1;
			if (X > Particle_Cel_Dim-2) X = Particle_Cel_Dim-2;
			if (Y < 1) Y = 1;
			if (Y > Particle_Cel_Dim - 2) Y = Particle_Cel_Dim - 2;

			// neighbour cells
			var CelCenter = ParticleCels[Y, X].Density;
			var CelLeft = ParticleCels[Y, X-1].Density;
			var CelRight = ParticleCels[Y, X+1].Density;
			var CelDown = ParticleCels[Y-1, X].Density;
			var CelUp = ParticleCels[Y+1, X].Density;

			Vector3 Dispersion = Vector3.zero;
			float Dc = 1f;
			Dispersion += Dc*(CelCenter-CelLeft)*new Vector3(-1f,0f,0f);
			Dispersion += Dc*(CelCenter-CelRight)*new Vector3(1f,0f,0f);
			Dispersion += Dc*(CelCenter-CelDown)*new Vector3(0f,-1f,0f);
			Dispersion += Dc*(CelCenter-CelUp)*new Vector3(0f,1f,0f);

			Vector3 ddP = particles[ParticleIndex].ddP+Dispersion;

			// calculate movement & color
			particles[ParticleIndex].P += (0.5f*Mathf.Sqrt(Time.deltaTime)*Time.deltaTime*ddP + Time.deltaTime*particles[ParticleIndex].dP);
			particles[ParticleIndex].dP += Time.deltaTime*ddP;
			particles[ParticleIndex].Col += Time.deltaTime*particles[ParticleIndex].dColor;

            // ground collision & bounce
			if (groundPlaneCollision && particles[ParticleIndex].P.y<groundY)
            {
                float CoefficientOfRestitution = 0.3f;
				float CoefficientOfFriction = 0.7f;
				particles[ParticleIndex].P.y = -particles[ParticleIndex].P.y;
				particles[ParticleIndex].dP.y = -CoefficientOfRestitution*particles[ParticleIndex].dP.y;
				particles[ParticleIndex].dP.x = CoefficientOfFriction*particles[ParticleIndex].dP.x;
			}

			/*
			// TEST max speed clamp
			if (particles[ParticleIndex].dP.sqrMagnitude>maxSpeed)
			{
				particles[ParticleIndex].dP = particles[ParticleIndex].dP.normalized*maxSpeed;
			}*/

			// clamp colors
			particles[ParticleIndex].Col.x = Clamp01(particles[ParticleIndex].Col.x);
			particles[ParticleIndex].Col.y = Clamp01(particles[ParticleIndex].Col.y);
			particles[ParticleIndex].Col.z = Clamp01(particles[ParticleIndex].Col.z);
			particles[ParticleIndex].Col.w = Clamp01(particles[ParticleIndex].Col.w);

            // fix particle sudden spawn by making it transparent first, not working smoothly
            if (particles[ParticleIndex].Col.w>0.9f)
            {
                particles[ParticleIndex].Col.w = 0.9f*Clamp01MapToRange(1f,(particles[ParticleIndex].Col.w),0.9f);
            }

			// set position, color, scale
			sprites[ParticleIndex].transform.position = particles[ParticleIndex].P;
			sprites[ParticleIndex].color = (Color)particles[ParticleIndex].Col;
            var s = particles[ParticleIndex].Col.w;
			sprites[ParticleIndex].transform.localScale = new Vector3(s, s, s)*0.75f;
		}
	}



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
