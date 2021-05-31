#include "Uniforms.glsl"
#include "Samplers.glsl"
#include "Transform.glsl"
#include "ScreenPos.glsl"
#include "Lighting.glsl"

varying vec2 vScreenPos;

uniform sampler2D tex;
float total_time = 10.0;
bool infinite = true;

#ifdef COMPILEPS
uniform float cElapsedTime;
#endif

float PI = 2.0 * asin(1.0);

struct WaveEmitter {
	vec2 mPosition; 
	float mAmplitude;
	float mVelocity;
	float mWavelength;

float GetPeriodTime() {
		return mWavelength / mVelocity;
	}
};

float emitter_size = 3.0;
WaveEmitter emitter[3];

float GetPhase(vec2 point, WaveEmitter emit, float time) {
	float distance = sqrt( pow(point.x - emit.mPosition.x,2) + pow(point.y - emit.mPosition.y, 2) );

	if (!infinite && distance / emit.mVelocity >= time) {
		return 0.0;
	} else {
		return sin((time / emit.GetPeriodTime() - distance / emit.mWavelength) * 2 * PI);
	}
}

vec2 transformCoord(vec2 orig) {
	vec2 final = orig;	
	total_time += cElapsedTime;
	for(int i = 0; i < emitter_size; ++i) {
		vec2 rel = orig - emitter[i].mPosition;
		float fac = GetPhase(orig, emitter[i], total_time) * emitter[i].mAmplitude;
		final += fac * rel;
	}
	return final;
}

vec4 transformColor(vec4 c, vec2 p) {
	float fac = 0;
	float a = 0;
	total_time += sin(cElapsedTime*1.0);
	for(int i = 0; i < emitter_size; ++i) {
		fac += GetPhase(p, emitter[i], total_time) * emitter[i].mAmplitude;
		a = emitter[i].mAmplitude;
	}
	fac = (fac / a + 1.0)/2.0;
	return c * fac;
}

void VS()
{
    mat4 modelMatrix = iModelMatrix;
    vec3 worldPos = GetWorldPos(modelMatrix);
    gl_Position = GetClipPos(worldPos);
    vScreenPos = GetScreenPosPreDiv(gl_Position);
}

void PS() {
	WaveEmitter emit0;
	emit0.mPosition = vec2(0.1,0.7);
	emit0.mAmplitude = 0.0023;
	emit0.mVelocity = 0.1;
	emit0.mWavelength = 0.2;
	emitter[0] = emit0;

	WaveEmitter emit1;
	emit1.mPosition = vec2(0.8,-0.1);
	emit1.mAmplitude = 0.0023;
	emit1.mVelocity = 0.13;
	emit1.mWavelength = 0.3;
	emitter[1] = emit1;

	WaveEmitter emit2;
	emit2.mPosition = vec2(1.1,0.9);
	emit2.mAmplitude = 0.0023;
	emit2.mVelocity = 0.05;
	emit2.mWavelength = 0.5;
	emitter[2] = emit2;

	vec2 coord = transformCoord(vScreenPos.st);	
	gl_FragColor = texture2D(tex, coord), coord;
}
