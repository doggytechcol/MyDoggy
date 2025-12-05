package com.doggytech.mydoggy;

import android.app.Activity;
import android.content.Intent;
import com.google.android.gms.auth.api.signin.GoogleSignIn;
import com.google.android.gms.auth.api.signin.GoogleSignInAccount;
import com.google.android.gms.auth.api.signin.GoogleSignInClient;
import com.google.android.gms.auth.api.signin.GoogleSignInOptions;
import com.google.android.gms.common.api.ApiException;
import com.google.android.gms.tasks.Task;
import com.unity3d.player.UnityPlayer;

public class GoogleLoginController {

    private static final int RC_SIGN_IN = 9001; // Código de solicitud
    private GoogleSignInClient mGoogleSignInClient;
    private Activity unityActivity;

    // Nombre del GameObject en Unity que recibirá los mensajes (AuthService)
    private static final String UNITY_GAMEOBJECT = "AuthService";
    
    // 🔥 Tu Web Client ID de Firebase
    private final String WEB_CLIENT_ID = "671856843241-ql6d59rkugk6uoeobah4j3hm343781pq.apps.googleusercontent.com";

    // ====================================================================
    // ☕ Constructor
    // ====================================================================

    public GoogleLoginController(Activity activity) {
        this.unityActivity = activity;
        initializeGoogleSignIn();
    }

    private void initializeGoogleSignIn() {
        // Solicitamos el ID Token para Firebase
        GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DEFAULT_SIGN_IN)
                .requestIdToken(WEB_CLIENT_ID)
                .requestEmail()
                .build();

        mGoogleSignInClient = GoogleSignIn.getClient(unityActivity, gso);
    }

    // ====================================================================
    // ☕ Métodos llamados desde C# (Unity)
    // ====================================================================

    /** Inicia el Intent nativo de Google Sign-In */
    public void signIn() {
        // Cerramos sesión por si acaso para asegurar una autenticación limpia
        signOut(); 
        
        Intent signInIntent = mGoogleSignInClient.getSignInIntent();
        // Llamamos a la actividad de Unity para iniciar la sub-actividad
        unityActivity.startActivityForResult(signInIntent, RC_SIGN_IN);
    }

    /** Cierra sesión de Google */
    public void signOut() {
        if (mGoogleSignInClient != null) {
            mGoogleSignInClient.signOut();
        }
    }

    // ====================================================================
    // ☕ Manejo de la Respuesta del Intent
    // ====================================================================

    /**
     * Llamado automáticamente por UnityPlayerActivity.
     * Captura el resultado del Intent.
     */
    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode == RC_SIGN_IN) {
            Task<GoogleSignInAccount> task = GoogleSignIn.getSignedInAccountFromIntent(data);
            handleSignInResult(task);
        }
    }

    private void handleSignInResult(Task<GoogleSignInAccount> completedTask) {
        try {
            GoogleSignInAccount account = completedTask.getResult(ApiException.class);
            
            // Éxito: Obtenemos el ID Token
            String idToken = account.getIdToken();
            
            // 🔥 ENVIAR ÉXITO A UNITY
            if (idToken != null) {
                // Unity llama al método LoginNativeGoogle de AuthService con el token
                UnityPlayer.UnitySendMessage(UNITY_GAMEOBJECT, "LoginNativeGoogle", idToken);
            } else {
                // Error: No se pudo obtener el token
                UnityPlayer.UnitySendMessage(UNITY_GAMEOBJECT, "GoogleLoginFailed", "Token nulo o inválido.");
            }

        } catch (ApiException e) {
            // Falla la autenticación (cancelada por el usuario, error de conexión, etc.)
            String errorMessage = "Error: " + e.getStatusCode() + " - " + e.getMessage();
            
            // 🔥 ENVIAR ERROR A UNITY
            UnityPlayer.UnitySendMessage(UNITY_GAMEOBJECT, "GoogleLoginFailed", errorMessage);
        }
    }
}