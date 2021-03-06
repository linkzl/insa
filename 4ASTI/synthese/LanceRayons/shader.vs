
// attribute: indique qu'on recupere les donnees une a une dans un tableau de sommets
attribute vec2 aVertexPosition;
attribute vec3 aPixCenterPosition;

// uniform: donnees recuperees qu'une seule fois
uniform mat3 uMVMatrix; // Model View: rotation
uniform mat3 uPMatrix;  // Projection

// 
varying vec3 pixCenter;


// ========= MAIN =========
void main(void) {
	pixCenter   = aPixCenterPosition * uMVMatrix;
	gl_Position =  vec4(aVertexPosition, 0.0, 1.0);
}
