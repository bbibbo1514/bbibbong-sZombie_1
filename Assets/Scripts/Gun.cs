using System.Collections;
using UnityEngine;

// 총을 구현
public class Gun : MonoBehaviour {
    // 총의 상태를 표현하는 데 사용할 타입을 선언
    public enum State {
        Ready, // 발사 준비됨
        Empty, // 탄알집이 빔
        Reloading // 재장전 중
    }

    public State state { get; private set; } // 현재 총의 상태

    public Transform fireTransform; // 탄알이 발사될 위치

    public ParticleSystem muzzleFlashEffect; // 총구 화염 효과
    public ParticleSystem shellEjectEffect; // 탄피 배출 효과

    private LineRenderer bulletLineRenderer; // 탄알 궤적을 그리기 위한 렌더러

    private AudioSource gunAudioPlayer; // 총 소리 재생기

    public GunData gunData; // 총의 현재 데이터

    private float fireDistance = 50f; // 사정거리

    public int ammoRemain = 100; // 남은 전체 탄알
    public int magAmmo; // 현재 탄알집에 남아 있는 탄알

    private float lastFireTime; // 총을 마지막으로 발사한 시점

    private void Awake() {
        // 사용할 컴포넌트의 참조 가져오기
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();

        // 사용할 점을 두 개로 변경
        bulletLineRenderer.positionCount = 2;
        // 라인 렌더러를 비활성화
        bulletLineRenderer.enabled = false;    
    }

    private void OnEnable() {
        // 전체 예비 탄알 양을 초기화
        ammoRemain = gunData.startAmmoRemain;

        // 현재 탄창을 가득 채우기
        magAmmo = gunData.magCapacity;

        // 총의 상태를 발사 준비 상태로 변경
        state = State.Ready;
        // 마지막으로 총을 쏜 시점을 초기화
        lastFireTime = 0;

    }

    // 발사 시도
    public void Fire() {
        if (state == State.Ready && Time.time >= lastFireTime + gunData.timeBetFire) {
            // 발사 시도
            lastFireTime = Time.time; // 마지막 발사 시점 갱신
            Shot();
        } else if (state == State.Empty) {
            // 재장전 시도
            Reload();
        }

    }

    // 실제 발사 처리
    private void Shot() 
    {
        RaycastHit hit;
        Vector3 hitPosition = Vector3.zero; // 발사한 총알이 맞은 위치
        if(Physics.Raycast(fireTransform.position, fireTransform.forward, out hit, fireDistance)) 
        {
            IDamageable target = hit.collider.GetComponent<IDamageable>(); // 충돌한 오브젝트의 IDamageable 인터페이스를 가져옴
            if (target != null) 
            {
                target.OnDamage(gunData.damage, hit.point, hit.normal);
            }
            hitPosition = hit.point;
        }
        else 
        {
            // 레이가 다른 물체와 충돌하지 않았다면
            // 탄알이 최대 사정거리까지 날아갔을 때의 위치를 충돌 위치로 사용
            hitPosition = fireTransform.position + fireTransform.forward * fireDistance;
        }
        // 발사 이펙트 재생 시작
        StartCoroutine(ShotEffect(hitPosition));

        // 남은 탄환 수를 -1
        magAmmo--;
        // 탄환이 0이 되면 총의 상태를 Empty로 변경
        if (magAmmo <= 0) 
        {
            state = State.Empty; // 총의 상태를 Empty로 변경         
        }
          
    }

    // 발사 이펙트와 소리를 재생하고 탄알 궤적을 그림
    private IEnumerator ShotEffect(Vector3 hitPosition) {

        muzzleFlashEffect.Play(); // 총구 화염 효과 재생
        shellEjectEffect.Play(); // 탄피 배출 효과 재생
        gunAudioPlayer.PlayOneShot(gunData.shotClip); // 총 소리 재생
        // 라인 렌더러를 활성화하여 탄알 궤적을 그림
        bulletLineRenderer.SetPosition(0, fireTransform.position); // 발사 위치 설정
        bulletLineRenderer.SetPosition(1, hitPosition); // 발사 방향 설정
        bulletLineRenderer.enabled = true;

        // 0.03초 동안 잠시 처리를 대기
        yield return new WaitForSeconds(0.03f);

        // 라인 렌더러를 비활성화하여 탄알 궤적을 지움
        bulletLineRenderer.enabled = false;
    }

    // 재장전 시도
    public bool Reload() {
        return false;
    }

    // 실제 재장전 처리를 진행
    private IEnumerator ReloadRoutine() {
        // 현재 상태를 재장전 중 상태로 전환
        state = State.Reloading;

        // 재장전 소리 재생
        gunAudioPlayer.PlayOneShot(gunData.reloadClip);

        // 재장전 소요 시간 만큼 처리 쉬기
        yield return new WaitForSeconds(gunData.reloadTime);

        // 탄창에 채울 탄알 계산
        int ammoToFill = gunData.magCapacity - magAmmo; // 채울 탄알 수

        // 탄창에 채워야 할 탄알이 남은 탄알보다 많다면
        // 채워야 할 탄알 수를 남은 탄알 수에 맞춰 줄임
        if (ammoToFill > ammoRemain) {
            ammoToFill = ammoRemain;
        }

        // 탄창을 채움
        magAmmo += ammoToFill;
        // 남은 탄알에서 탄창에 채운만큼 탄알을  뺌

        // 총의 현재 상태를 발사 준비된 상태로 변경
        state = State.Ready;
    }
}