;; macro to quickly define a function
(defmacro defun (lambda (name binds &rest body)
    (if (and (> (length body) 1) (string? (first body)))
      `(define ,name (lambda ,binds ,@(nth body 1)))
      `(define ,name (lambda ,binds ,@body))
      )
    )
  )

(defmacro cond (lambda (&rest xs)
  (if (> (length xs) 0)
      (list 'if (first xs)
        (if (> (length xs) 1)
          (nth xs 1)
          (throw "odd number of forms to cond")
          )
          (cons 'cond (rest (rest xs)))
        )
      )
    )
  )

(defmacro not (lambda (x) `(if ,x #f #t)))

(defun +1! (in)
    (+ in 1)
  )

(defun ! (x)
  (if (= x 0)
    1
    (* x (! (- x 1)))
    )
  )