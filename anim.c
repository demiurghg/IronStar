struct Animation {
  int state; // STATE_IDLE, STATE_FIRE, STATE_RELOAD...
  int length;        // кол-во фреймов в анимации
  int currentFrame;  // текущий кадр
  int nextAnimation; // индекс следующей анимации
  int nextFrame;     // с какого фрейма
  int targetState;   // пытаемся перейти в этот стейт
  
  int transCount;    // кол-во переходов
  
  struct Transition {
    int state;   // в какой стейт переводит
    int loFrame; // с какого фрейма сработает
    int hiFrame; // по какой
  } *trans;
 
  void update() {
    currentFrame++;
    
    if (state != targetState) {
      for (int i = 0; i < transCount; i++) {
        if (trans[i].state == targetState &&
            trans[i].loFrame <= currentFrame &&
            trans[i].hiFrame >= currentFrame) {
            // нашли подходящий переход
            setAnimation(trans[i].nextAnimation, trans[i].nextFrame);            
            return;
        }
      }
    }
    
    // анимация кончилась, переходим на следующую
    if (currentFrame >= length) {
        setAnimation(nextAnimation, nextFrame);
    }
  }  
  
  void setAnimation(int animIndex, int animFrame) {
    Animation &anim = GlobalAnimations[animIndex];
    *this = anim;
    currentFrame = animFrame;
  }
}